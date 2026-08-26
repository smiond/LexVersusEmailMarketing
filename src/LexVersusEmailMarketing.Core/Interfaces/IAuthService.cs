using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Entities;

namespace LexVersusEmailMarketing.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);
}

public interface IUnsubscribeService
{
    Task<UnsubscribeInfoDto?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<UnsubscribeResultDto> UnsubscribeAsync(string token, CancellationToken cancellationToken = default);
}
