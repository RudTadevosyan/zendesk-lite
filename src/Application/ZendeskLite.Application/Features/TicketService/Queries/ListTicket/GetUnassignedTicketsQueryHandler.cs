using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListTicket
{
    public class GetUnassignedTicketsQueryHandler : IRequestHandler<GetUnassignedTicketsQuery, Result<PagedResult<BaseTicketDto>>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketAuditRepository _auditRepository;
        private readonly ILogger<GetUnassignedTicketsQueryHandler> _logger;

        public GetUnassignedTicketsQueryHandler(
            ITicketRepository ticketRepository,
            ITicketAuditRepository auditRepository,
            ILogger<GetUnassignedTicketsQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _auditRepository = auditRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<BaseTicketDto>>> Handle(GetUnassignedTicketsQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Fetching unassigned tickets for page {Page}", request.Page);

            var pagedTickets = await _ticketRepository.GetUnassignedTicketsAsync(request.Page, request.PageSize, ct);

            var dtos = new List<BaseTicketDto>();

            foreach (var t in pagedTickets.Items)
            {
                var logs = await _auditRepository.GetLogsByTicketIdAsync(t.Id, ct);

                dtos.Add(new BaseTicketDto(
                    t.Id,
                    t.Title,
                    t.RawDescription,
                    t.CleanedDescription,
                    t.Status,
                    t.Category,
                    t.Comments,
                    t.CreatedAt
                ));
            }

            _logger.LogInformation("Successfully retrieved {Count} unassigned tickets.", dtos.Count);

            return Result.Success(new PagedResult<BaseTicketDto>(
                dtos, pagedTickets.TotalCount, request.Page, request.PageSize));
        }
    }
}