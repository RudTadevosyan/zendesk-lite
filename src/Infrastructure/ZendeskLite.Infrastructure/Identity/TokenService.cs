using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ZendeskLite.Application.Common.Interfaces;
using ZendeskLite.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ZendeskLite.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly IDistributedCache _cache;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<TokenService> _logger; 
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public TokenService(IDistributedCache cache, IConfiguration config, UserManager<AppUser> userManager, ILogger<TokenService> logger)
    {
        _cache = cache;
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

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

        await _cache.SetStringAsync($"refresh:{hashedToken}", user.Id, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        }, ct);

        _logger.LogInformation("Refresh token stored for user: {UserId}", user.Id);
        return Result.Success(new TokenResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(30)));
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        var userId = await _cache.GetStringAsync($"refresh:{hashedToken}", ct);
        if (userId is null)
            return Result.Failure<TokenResponse>(Error.NotFound("Token.Invalid", "Refresh token is invalid or expired."));

        await _cache.RemoveAsync($"refresh:{hashedToken}", ct);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<TokenResponse>(Error.NotFound("User.NotFound", "User no longer exists."));

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", userId);
        return await GenerateTokenAsync(user, ct);
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        await _cache.RemoveAsync($"refresh:{hashedToken}", ct);

        _logger.LogInformation("Refresh token revoked for hash: {Hash}", hashedToken);
        return Result.Success();
    }
}