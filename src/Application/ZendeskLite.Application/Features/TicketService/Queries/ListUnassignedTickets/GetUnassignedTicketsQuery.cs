using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListUnassignedTickets;

public record GetUnassignedTicketsQuery(int Page = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<BaseTicketDto>>>;
