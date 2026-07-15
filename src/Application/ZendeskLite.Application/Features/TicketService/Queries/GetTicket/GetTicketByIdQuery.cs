using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetTicket
{
    public record GetTicketByIdQuery(Guid Id, string CustomerId) : IRequest<Result<BaseTicketDto>>;
}
