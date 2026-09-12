using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZendeskLite.Application.Common.Extensions;
using ZendeskLite.Application.DTOs.Request.Agent;
using ZendeskLite.Application.Features.AgentService.Command.ChangeAgentAvailability;
using ZendeskLite.Application.Features.AgentService.Queries.GetAgentWorkloads;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/agents")]
    public class AgentController : ControllerBase
    {
        private readonly ISender _sender;
        public AgentController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("workloads")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> GetAgentWorkloads(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        {
            var query = new GetAgentWorkloadsQuery(pageNumber, pageSize);
            var result = await _sender.Send(query, ct);

            return result.Match(
                onSuccess: workloads => Ok(workloads),
                onFailure: HandleError
            );
        }

        [HttpPatch("availability")]
        [Authorize(Roles = "Admin,Agent")] 
        public async Task<IActionResult> ChangeAgentAvailability([FromBody] ChangeAgentAvailabilityRequestBody requestBody,
        CancellationToken ct = default)
        {
            var command = new ChangeAgentAvailabilityCommand(
                requestBody.IsAvailable,
                requestBody.TargetAgentId
            );

            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: () => Ok(),
                onFailure: HandleError
            );
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
}