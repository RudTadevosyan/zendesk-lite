using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs.Request.Agent
{
    public record ChangeAgentAvailabilityRequestBody(bool IsAvailable, string? TargetAgentId = null);

}
