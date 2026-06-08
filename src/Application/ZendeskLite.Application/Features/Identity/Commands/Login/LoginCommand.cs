using MediatR;
using ZendeskLite.Application.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<Result<TokenResponse>>;
}
