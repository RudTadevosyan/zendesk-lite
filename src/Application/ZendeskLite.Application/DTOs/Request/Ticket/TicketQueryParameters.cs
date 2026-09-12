using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.DTOs.Request.Ticket
{
    public record TicketQueryParameters(string? UserId, string? AgentId, TicketStatus? Status, TicketPriority? Priority,
        TicketCategory? Category, bool IsAssigned = true, int PageNumber = 1, int PageSize = 10);
}
