using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.AgentService.Queries.GetAgentWorkloads;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.AgentService.Queries.GetWorkloads;

public class GetAgentWorkloadsQueryHandler : IRequestHandler<GetAgentWorkloadsQuery, Result<IEnumerable<AgentWorkloadDto>>>
{
    private readonly IAgentRepository _agentRepository;

    public GetAgentWorkloadsQueryHandler(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<Result<IEnumerable<AgentWorkloadDto>>> Handle(GetAgentWorkloadsQuery request, CancellationToken ct)
    {
        var agents = await _agentRepository.GetAllAgentsAsync(ct);

        // Map domain entities to the application DTO here
        var workloads = agents.Select(u => new AgentWorkloadDto(
            u.Id,
            u.FirstName ?? string.Empty,
            u.LastName ?? string.Empty,
            u.Email ?? string.Empty,
            u.AgentSpecialty,
            u.IsAvailable,
            u.ActiveTicketCount
        ));

        return Result.Success(workloads);
    }
}