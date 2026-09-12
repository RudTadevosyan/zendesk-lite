using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs.Response;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListTickets
{
    public record ListTicketsQuery(string? UserId, string? AgentId, TicketStatus? Status, TicketPriority? Priority,
        TicketCategory? Category, bool IsAssigned = true, int PageNumber = 1, int PageSize = 10): IRequest<Result<PagedResult<BaseTicketDto>>>;
}
