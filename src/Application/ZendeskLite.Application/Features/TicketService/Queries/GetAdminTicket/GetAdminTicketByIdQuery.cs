using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetAdminTicket
{
    public record GetAdminTicketByIdQuery(Guid Id) : IRequest<Result<AdminTicketDto>>;
}
