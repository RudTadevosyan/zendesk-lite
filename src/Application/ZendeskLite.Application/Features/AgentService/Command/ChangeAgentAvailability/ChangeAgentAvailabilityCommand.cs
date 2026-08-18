using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.AgentService.Command.ChangeAgentAvailability
{
    public record ChangeAgentAvailabilityCommand(bool IsAvailable, string? TargetAgentId = null) : IRequest<Result>;
}
