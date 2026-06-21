using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Register
{
    public record RegisterCommand(string FirstName, string LastName, string Email, string Password, string ConfirmPassword) : IRequest<Result<TokenResponse>>
    {
    }
}
