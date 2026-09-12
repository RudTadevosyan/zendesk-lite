using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.DTOs.Request.Ticket
{
    public record UpdateTicketStatusRequestBody(TicketStatus NewStatus, string Notes);

}
