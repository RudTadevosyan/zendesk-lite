using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.TicketService.Queries.GetTicket;
using ZendeskLite.Domain.Common;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Result<BaseTicketDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<GetTicketByIdQueryHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public GetTicketByIdQueryHandler(
        ITicketRepository ticketRepository,
        ILogger<GetTicketByIdQueryHandler> logger,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<BaseTicketDto>> Handle(GetTicketByIdQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Handling GetTicketByIdQuery for Ticket ID: {TicketId} for User: {UserId}", request.Id, _currentUser.UserId);

        var ticket = await _ticketRepository.GetByIdAsync(request.Id, ct);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket not found for ID: {TicketId}", request.Id);
            return Result.Failure<BaseTicketDto>(Error.NotFound("404", "Ticket not found"));
        }

        if (ticket.CustomerId != _currentUser.UserId && !_currentUser.IsAdminOrAgent)
        {
            _logger.LogWarning("Unauthorized access attempt on Ticket ID: {TicketId} by User: {UserId}", request.Id, _currentUser.UserId);
            return Result.Failure<BaseTicketDto>(Error.Validation("403", "You are not authorized to view this ticket"));
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