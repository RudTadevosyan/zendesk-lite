using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Commands
{
    public record SubmitTicketCommand(string Title, string Description, string CustomerId) : IRequest<Result<Guid>>;
}
