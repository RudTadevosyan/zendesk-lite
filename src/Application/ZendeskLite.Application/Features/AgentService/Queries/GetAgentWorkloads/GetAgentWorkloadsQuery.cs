using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs.Response;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.AgentService.Queries.GetAgentWorkloads
{
    public record GetAgentWorkloadsQuery(int PageNumber = 1, int PageSize = 10)
            : IRequest<Result<PagedResult<AgentWorkloadDto>>>;
}
