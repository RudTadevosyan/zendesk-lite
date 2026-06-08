using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Common.Interfaces;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Features.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<TokenResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Attempting login for user: {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Invalid login attempt for email: {Email}", request.Email);
            return Result.Failure<TokenResponse>(Error.Failure("Auth.Invalid", "Invalid credentials."));
        }

        var result = await _tokenService.GenerateTokenAsync(user, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
        }
        else
        {
            _logger.LogError("Token generation failed for user {UserId}. Error: {ErrorCode}", user.Id, result.Error?.Code);
        }

        return result;
    }
}