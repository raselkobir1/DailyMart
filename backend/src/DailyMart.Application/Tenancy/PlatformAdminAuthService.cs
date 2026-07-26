using DailyMart.Application.Auth;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;

namespace DailyMart.Application.Tenancy;

public class PlatformAdminAuthService : IPlatformAdminAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher<PlatformAdmin> _passwordHasher;

    public PlatformAdminAuthService(
        IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator, IPasswordHasher<PlatformAdmin> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<PlatformAdminAuthResponseDto> LoginAsync(
        PlatformAdminLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var admin = (await _unitOfWork.Repository<PlatformAdmin>()
            .FindAsync(a => a.Username == request.Username, cancellationToken)).FirstOrDefault();

        if (admin is null || !admin.IsActive)
        {
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        var accessToken = _jwtTokenGenerator.GeneratePlatformAdminAccessToken(admin);

        return new PlatformAdminAuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(_jwtTokenGenerator.PlatformAdminAccessTokenLifetime),
            Username = admin.Username,
            FullName = admin.FullName
        };
    }
}
