using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Abstractions.Common.Interfaces;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public interface ITokenService
{
    Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct = default);
    Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default); 
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, string currentUserId, CancellationToken ct = default);
    Task<Result> RevokeAllUserRefreshTokensAsync(string userId, CancellationToken ct = default);
    Task<Result> RevokeAccessTokenAsync(string accessToken, CancellationToken ct); // with jwt blacklist 

}