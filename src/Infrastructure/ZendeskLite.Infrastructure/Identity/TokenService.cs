using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ZendeskLite.Domain.Common;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using StackExchange.Redis; 

namespace ZendeskLite.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<TokenService> _logger;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public TokenService(
        IDistributedCache cache,
        IConnectionMultiplexer redisConnection,
        IConfiguration config,
        UserManager<AppUser> userManager,
        ILogger<TokenService> logger)
    {
        _cache = cache;
        _redisConnection = redisConnection;
        _userManager = userManager;
        _logger = logger;

        // Fail fast if configuration is missing
        _jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in configuration.");
        _jwtIssuer = config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing.");
        _jwtAudience = config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing.");
    }

    public async Task<Result<TokenResponse>> GenerateTokenAsync(AppUser user, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var securityStamp = await _userManager.GetSecurityStampAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("AspNet.Identity.SecurityStamp", securityStamp ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));


        // Redis Set - User session indexing 
        // Store the individual refresh token mapping
        await _cache.SetStringAsync($"refresh:{hashedToken}", user.Id, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        }, ct);

        // Add the token hash to the user's tracking set for fast global invalidation
        var db = _redisConnection.GetDatabase();
        await db.SetAddAsync($"user-tokens:{user.Id}", hashedToken);

        _logger.LogInformation("Refresh token stored and indexed for user: {UserId}", user.Id);
        return Result.Success(new TokenResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(30)));
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var userId = await _cache.GetStringAsync($"refresh:{hashedToken}", ct);
        if (userId is null)
        {
            _logger.LogWarning("Refresh attempt with invalid or expired token hash: {Hash}", hashedToken);
            return Result.Failure<TokenResponse>(Error.NotFound("Token.Invalid", "Refresh token is invalid or expired."));
        }

        await _cache.RemoveAsync($"refresh:{hashedToken}", ct);
        var db = _redisConnection.GetDatabase();
        await db.SetRemoveAsync($"user-tokens:{userId}", hashedToken);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogError("Refresh failed: User {UserId} associated with token no longer exists.", userId);
            return Result.Failure<TokenResponse>(Error.NotFound("User.NotFound", "User account not found."));
        }

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);

        return await GenerateTokenAsync(user, ct);
    }

    public async Task<Result> RevokeAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(accessToken))
        {
            return Result.Failure(Error.Validation("Token.Invalid", "Invalid access token format."));
        }

        var jwtToken = handler.ReadJwtToken(accessToken);
        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
        {
            return Result.Failure(Error.Validation("Token.MissingJti", "Access token is missing the required JTI claim."));
        }

        var timeRemaining = jwtToken.ValidTo - DateTime.UtcNow;

        if (timeRemaining > TimeSpan.Zero)
        {
            await _cache.SetStringAsync($"blacklist:{jti}", "revoked", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeRemaining
            }, ct);

            _logger.LogInformation("Access token with JTI {Jti} successfully revoked and blacklisted until {Expiry}", jti, jwtToken.ValidTo);
        }

        return Result.Success();
    }

    public async Task<Result> RevokeAllUserRefreshTokensAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Revoking all indexed refresh tokens for user: {UserId}", userId);

        var db = _redisConnection.GetDatabase();
        var setKey = $"user-tokens:{userId}";

        // Get all token hashes belonging to this user instantly from their set
        RedisValue[] tokenHashes = await db.SetMembersAsync(setKey);

        if (tokenHashes.Length > 0)
        {
            // Build array of keys to delete
            var keysToDelete = tokenHashes.Select(th => (RedisKey)$"refresh:{th}").ToArray();

            // Delete all individual token keys and the user set atomically/in bulk - (avoid N+1)
            await db.KeyDeleteAsync(keysToDelete);
        }

        // 3. Delete the tracking set itself
        await db.KeyDeleteAsync(setKey);

        _logger.LogInformation("Successfully revoked all refresh tokens for user: {UserId}", userId);
        return Result.Success();
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string currentUserId, CancellationToken ct = default)
    {
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var key = $"refresh:{hashedToken}";

        var ownerId = await _cache.GetStringAsync(key, ct);

        if (ownerId == null)
            return Result.Failure(Error.NotFound("Token.NotFound", "Token not found."));

        if (ownerId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to revoke a token belonging to {OwnerId}!", currentUserId, ownerId);
            return Result.Failure(Error.Validation("Auth.Forbidden", "You cannot revoke a token that does not belong to you."));
        }

        await _cache.RemoveAsync(key, ct);
        var db = _redisConnection.GetDatabase();
        await db.SetRemoveAsync($"user-tokens:{currentUserId}", hashedToken);

        _logger.LogInformation("Refresh token revoked for user: {UserId}", currentUserId);
        return Result.Success();
    }
}