namespace LexVersusEmailMarketing.Core.DTOs;

public record RegisterDto(string Email, string Password, string FullName);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(string Token, UserDto User);

public record UserDto(Guid Id, string Email, string FullName);

public record UnsubscribeInfoDto(string Email, bool IsSubscribed, DateTime? UnsubscribedAt);

public record UnsubscribeResultDto(bool Success, string Message);
