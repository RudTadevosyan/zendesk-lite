using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs
{
    public record TicketSubmittedEvent(Guid TicketId);
}
