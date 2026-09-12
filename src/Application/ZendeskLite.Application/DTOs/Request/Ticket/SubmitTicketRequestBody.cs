using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs.Request.Ticket
{
    public record SubmitTicketRequestBody(string Title, string Description);

}
