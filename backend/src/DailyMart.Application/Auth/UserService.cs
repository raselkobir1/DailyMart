using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Rbac;
using Microsoft.AspNetCore.Identity;

namespace DailyMart.Application.Auth;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<User, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : user => user.Username.Contains(request.SearchTerm) || user.FullName.Contains(request.SearchTerm);

        var result = await _userRepository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = result.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<UserDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<UserDto> CreateAsync(
        CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureUsernameIsUniqueAsync(request.Username, excludeId: null, cancellationToken);
        await EnsureRoleExistsAsync(request.Role, cancellationToken);

        var user = new User
        {
            Username = request.Username,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(
        long id, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await GetEntityAsync(id, cancellationToken);

        await EnsureRoleExistsAsync(request.Role, cancellationToken);

        user.FullName = request.FullName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            throw new BusinessRuleException("You cannot delete your own account.");
        }

        var user = await GetEntityAsync(id, cancellationToken);

        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(User), id);

    private async Task EnsureUsernameIsUniqueAsync(string username, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();

        var duplicateExists = await _userRepository.ExistsAsync(
            user => user.Username.ToLower() == normalizedUsername && (excludeId == null || user.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A user with username '{username}' already exists.");
        }
    }

    private async Task EnsureRoleExistsAsync(string role, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Role>().ExistsAsync(r => r.Name == role, cancellationToken))
        {
            throw new BusinessRuleException($"Role '{role}' does not exist.");
        }
    }
}
