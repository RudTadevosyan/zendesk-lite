using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Features.TicketService.Queries
{
    public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PagedResult<Ticket>>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<GetTicketsQueryHandler> _logger;

        public GetTicketsQueryHandler(ITicketRepository ticketRepository, ILogger<GetTicketsQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<Ticket>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching tickets for user: {UserId}", request.UserId);

            var parameters = new TicketQueryParameters(
                request.UserId,
                request.Status,
                request.Priority,
                request.Page,
                request.PageSize
            );

            var result = await _ticketRepository.GetFilteredAsync(parameters, cancellationToken);

            return Result.Success(result);

        }
    }
}
