using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs
{
    // For user view
    public record TicketDto(
    Guid Id,
    string Title,
    string RawDescription,
    string Status,
    DateTime CreatedAt);
}
