using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs.Response;
using ZendeskLite.Application.Features.AgentService.Queries.GetAgentWorkloads;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.AgentService.Queries.GetWorkloads
{
    public class GetAgentWorkloadsQueryHandler : IRequestHandler<GetAgentWorkloadsQuery, Result<PagedResult<AgentWorkloadDto>>>
    {
        private readonly IAgentRepository _agentRepository;

        public GetAgentWorkloadsQueryHandler(IAgentRepository agentRepository)
        {
            _agentRepository = agentRepository;
        }

        public async Task<Result<PagedResult<AgentWorkloadDto>>> Handle(GetAgentWorkloadsQuery request, CancellationToken ct)
        {
            int pageNumber = request.PageNumber;
            int pageSize = request.PageSize;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var pagedAgents = await _agentRepository.GetAllAgentsAsync(pageNumber, pageSize, ct);

            var workloadDtos = pagedAgents.Items.Select(u => new AgentWorkloadDto(
                u.Id,
                u.FirstName ?? string.Empty,
                u.LastName ?? string.Empty,
                u.Email ?? string.Empty,
                u.AgentSpecialty,
                u.IsAvailable,
                u.ActiveTicketCount
            )).ToList();

            var pagedResult = new PagedResult<AgentWorkloadDto>(
                workloadDtos,
                pagedAgents.TotalCount,
                pagedAgents.Page,
                pagedAgents.PageSize
            );

            return Result.Success(pagedResult);
        }
    }
}