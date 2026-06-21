using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Abstractions.Common.Interfaces;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public interface ITokenService
{
    Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct = default);
    Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}