namespace DailyMart.Application.Rbac;

/// <summary>Unpaginated - Menus is a small, fully admin-managed configuration set (rarely more than a few
/// dozen rows), and both the Menus management screen and the Permissions matrix need the complete list
/// every time to build their tree/table - same reasoning as IProductService.GetAllForExportAsync.</summary>
public interface IMenuService
{
    Task<IReadOnlyList<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MenuDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Also grants the system "Admin" role full CRUD on the new menu, so a newly added screen is
    /// never invisible to Admin pending a manual permissions step.</summary>
    Task<MenuDto> CreateAsync(CreateMenuRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Never touches Key - see Menu's doc comment. Throws BusinessRuleException if ParentId
    /// would make the menu its own ancestor or descendant.</summary>
    Task<MenuDto> UpdateAsync(long id, MenuRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException if this menu has any child menus - they'd otherwise be left
    /// pointing at a ParentId that no longer resolves.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
