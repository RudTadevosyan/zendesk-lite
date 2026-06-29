using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Domain.Enums;


namespace ZendeskLite.Application.Features.TicketService.Queries
{
    public record GetTicketsQuery(string? UserId, TicketStatus? Status, TicketPriority? Priority,
            int Page = 1, int PageSize = 10) : IRequest<Result<PagedResult<Ticket>>>;


}
