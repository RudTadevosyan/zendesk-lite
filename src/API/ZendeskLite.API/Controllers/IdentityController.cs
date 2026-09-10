using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZendeskLite.Application.Common.Extensions;
using ZendeskLite.Application.Features.Identity.Commands.ChangePassword;
using ZendeskLite.Application.Features.Identity.Commands.Login;
using ZendeskLite.Application.Features.Identity.Commands.Logout;
using ZendeskLite.Application.Features.Identity.Commands.Refresh;
using ZendeskLite.Application.Features.Identity.Commands.Register;
using ZendeskLite.Application.Features.Identity.Commands.Revoke;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.API.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly ISender _sender;

    public IdentityController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.Match(
            onSuccess: tokenResponse => Ok(tokenResponse),
            onFailure: HandleError
        );
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.Match(
            onSuccess: tokenResponse => Ok(tokenResponse),
            onFailure: HandleError
        );
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.Match(
            onSuccess: tokenResponse => Ok(tokenResponse),
            onFailure: HandleError
        );
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return result.Match(
            onSuccess: () => Ok(),
            onFailure: HandleError
        );
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestBody requestBody, CancellationToken ct)
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token") ?? string.Empty;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var command = new LogoutCommand(accessToken, requestBody.RefreshToken, currentUserId);
        var result = await _sender.Send(command, ct);

        return result.Match(
            onSuccess: () => Ok(),
            onFailure: HandleError
        );
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestBody requestBody, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var accessToken = await HttpContext.GetTokenAsync("access_token") ?? string.Empty;

        var command = new ChangePasswordCommand(currentUserId, accessToken, requestBody.CurrentPassword, requestBody.NewPassword);
        var result = await _sender.Send(command, ct);

        return result.Match(
            onSuccess: () => Ok(),
            onFailure: HandleError
        );
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized(new { message = "Invalid token claims." });
        }

        return Ok(new
        {
            Id = userId,
            Email = email,
            Message = "Successfully authenticated via JWT!"
        });
    }
    private IActionResult HandleError(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(error),
            ErrorType.Validation => BadRequest(error),
            ErrorType.Conflict => Conflict(error),
            _ => BadRequest(error)
        };
    }
}

public record LogoutRequestBody(string? RefreshToken);
public record ChangePasswordRequestBody(string CurrentPassword, string NewPassword);