using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZendeskLite.Application.Common.Extensions;
using ZendeskLite.Application.Features.Identity.Commands.Login;
using ZendeskLite.Application.Features.Identity.Commands.Refresh;
using ZendeskLite.Application.Features.Identity.Commands.Register;
using ZendeskLite.Application.Features.Identity.Commands.Revoke;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        => (await _mediator.Send(command)).Match(Ok, BadRequest);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
        => (await _mediator.Send(command)).Match(Ok, BadRequest);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshCommand command)
        => (await _mediator.Send(command)).Match(Ok, BadRequest);

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeCommand command)
        => (await _mediator.Send(command)).Match(NoContent, BadRequest);

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMyInfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new
        {
            Message = "You are successfully authenticated!",
            UserId = userId,
            Email = email
        });
    }
}