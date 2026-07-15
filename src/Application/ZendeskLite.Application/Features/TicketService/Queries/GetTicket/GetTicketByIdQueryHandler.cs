using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetTicket
{
    public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Result<BaseTicketDto>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<GetTicketByIdQueryHandler> _logger;

        public GetTicketByIdQueryHandler(ITicketRepository ticketRepository, ILogger<GetTicketByIdQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result<BaseTicketDto>> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling GetTicketByIdQuery for Ticket ID: {TicketId}", request.Id);

            var ticket = await _ticketRepository.GetByIdAsync(request.Id, cancellationToken);

            _logger.LogInformation("Retrieved Ticket: {@Ticket}", ticket);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket not found for ID: {TicketId}", request.Id);
                return Result.Failure<BaseTicketDto>(Error.NotFound("404", "Ticket not found"));
            }

            if (ticket.CustomerId != request.CustomerId)
            {
                _logger.LogWarning("Customer ID mismatch for Ticket ID: {TicketId}. Expected: {ExpectedCustomerId}, Actual: {ActualCustomerId}", request.Id, request.CustomerId, ticket.CustomerId);
                return Result.Failure<BaseTicketDto>(Error.Validation("403", "You are not authorized"));
            }

            return Result.Success(new BaseTicketDto(
                ticket.Id,
                ticket.Title,
                ticket.RawDescription,
                ticket.CleanedDescription,
                ticket.Status,
                ticket.Category,
                ticket.Comments,
                ticket.CreatedAt
            ));
        }
    }
}
