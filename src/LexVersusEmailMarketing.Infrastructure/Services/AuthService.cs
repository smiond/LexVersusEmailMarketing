using System.Text.RegularExpressions;
using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;
using LexVersusEmailMarketing.Core.Interfaces;
using LexVersusEmailMarketing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LexVersusEmailMarketing.Infrastructure.Services;

public partial class AuthService(AppDbContext db, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(email))
        {
            throw new InvalidOperationException("Invalid email address.");
        }

        if (dto.Password.Length < 8)
        {
            throw new InvalidOperationException("Password must be at least 8 characters.");
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = dto.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken);
        return user is null ? null : new UserDto(user.Id, user.Email, user.FullName);
    }

    private AuthResponseDto CreateAuthResponse(AppUser user)
    {
        var token = jwtTokenService.GenerateToken(user);
        return new AuthResponseDto(token, new UserDto(user.Id, user.Email, user.FullName));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
