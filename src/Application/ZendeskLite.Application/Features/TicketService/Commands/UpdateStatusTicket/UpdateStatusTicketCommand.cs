using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.Features.TicketService.Commands.UpdateStatusTicket
{   public record UpdateTicketStatusCommand(
        Guid TicketId,
        TicketStatus NewStatus,
        string AgentId,
        bool IsAdmin,
        string Notes) : IRequest<Result>;
}
