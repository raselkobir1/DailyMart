using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Rbac;

namespace DailyMart.Application.Rbac;

public class MenuService : IMenuService
{
    private readonly IUnitOfWork _unitOfWork;

    public MenuService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Menu> Repository => _unitOfWork.Repository<Menu>();

    public async Task<IReadOnlyList<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var menus = await Repository.GetAllAsync(cancellationToken);
        return menus.OrderBy(m => m.SortOrder).Select(m => m.ToDto()).ToList();
    }

    public async Task<MenuDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<MenuDto> CreateAsync(
        CreateMenuRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureKeyIsUniqueAsync(request.Key, excludeId: null, cancellationToken);
        await EnsureParentExistsAsync(request.ParentId, cancellationToken);

        var menu = request.ToEntity();
        await Repository.AddAsync(menu, cancellationToken);
        // Saved now so menu.Id is populated before it's used as the permission grant's foreign key -
        // same two-phase reasoning as Purchase/Supplier creation elsewhere in this codebase.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await GrantAdminFullAccessAsync(menu.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return menu.ToDto();
    }

    public async Task<MenuDto> UpdateAsync(
        long id, MenuRequestDto request, CancellationToken cancellationToken = default)
    {
        var menu = await GetEntityAsync(id, cancellationToken);

        await EnsureParentExistsAsync(request.ParentId, cancellationToken);
        await EnsureNoCycleAsync(id, request.ParentId, cancellationToken);

        request.ApplyTo(menu);

        Repository.Update(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return menu.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var menu = await GetEntityAsync(id, cancellationToken);

        var hasChildren = await Repository.ExistsAsync(m => m.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            throw new BusinessRuleException(
                $"Menu '{menu.Label}' has sub-menus and cannot be deleted - move or delete those first.");
        }

        Repository.Remove(menu);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Menu> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Menu), id);

    private async Task EnsureKeyIsUniqueAsync(string key, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            menu => menu.Key.ToLower() == normalizedKey && (excludeId == null || menu.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A menu with key '{key}' already exists.");
        }
    }

    private async Task EnsureParentExistsAsync(long? parentId, CancellationToken cancellationToken)
    {
        if (parentId is not null && !await Repository.ExistsAsync(m => m.Id == parentId, cancellationToken))
        {
            throw new BusinessRuleException($"Parent menu with id '{parentId}' does not exist.");
        }
    }

    /// <summary>Walks up the proposed parent's own ancestor chain - if this menu's id ever appears, the
    /// new ParentId would make it its own descendant/ancestor, an unresolvable cycle for the sidebar tree.</summary>
    private async Task EnsureNoCycleAsync(long menuId, long? proposedParentId, CancellationToken cancellationToken)
    {
        var currentId = proposedParentId;
        while (currentId is not null)
        {
            if (currentId == menuId)
            {
                throw new BusinessRuleException("A menu cannot be its own ancestor.");
            }

            var parent = await Repository.GetByIdAsync(currentId.Value, cancellationToken);
            currentId = parent?.ParentId;
        }
    }

    private async Task GrantAdminFullAccessAsync(long menuId, CancellationToken cancellationToken)
    {
        var adminRole = (await _unitOfWork.Repository<Role>()
            .FindAsync(r => r.Name == "Admin", cancellationToken))
            .FirstOrDefault();

        if (adminRole is null)
        {
            return;
        }

        await _unitOfWork.Repository<RoleMenuPermission>().AddAsync(new RoleMenuPermission
        {
            RoleId = adminRole.Id,
            MenuId = menuId,
            CanView = true,
            CanCreate = true,
            CanEdit = true,
            CanDelete = true
        }, cancellationToken);
    }
}
