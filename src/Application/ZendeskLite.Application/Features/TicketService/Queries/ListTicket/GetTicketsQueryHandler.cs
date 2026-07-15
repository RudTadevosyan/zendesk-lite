using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListTicket
{
    public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PagedResult<BaseTicketDto>>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<GetTicketsQueryHandler> _logger;

        public GetTicketsQueryHandler(ITicketRepository ticketRepository, ILogger<GetTicketsQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<BaseTicketDto>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching tickets for user: {UserId}", request.UserId);

            var parameters = new TicketQueryParameters(
                request.UserId,
                request.Status,
                request.Priority,
                request.Page,
                request.PageSize
            );

            var result = await _ticketRepository.GetFilteredTicketsAsync(parameters, cancellationToken);

            var dto = new PagedResult<BaseTicketDto>(
                result.Items.Select(t => new BaseTicketDto(
                    t.Id,
                    t.Title,
                    t.RawDescription,
                    t.CleanedDescription,
                    t.Status,
                    t.Category,
                    t.Comments,
                    t.CreatedAt
                )).ToList(),
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return Result.Success(dto);
        }
    }
}
