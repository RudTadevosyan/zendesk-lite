using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Revoke
{
    public record RevokeCommand(string RefreshToken) : IRequest<Result>;
}
