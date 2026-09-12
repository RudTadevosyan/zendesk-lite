using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Events
{
    public record TicketSubmittedEvent(Guid TicketId);
}
