using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.DTOs
{
    // For user view
    public record BaseTicketDto(
    Guid Id,
    string Title,
    string RawDescription,
    string? CleanedDescription,
    TicketStatus Status,
    TicketCategory Category,
    string? Comments,
    DateTimeOffset CreatedAt);
}

