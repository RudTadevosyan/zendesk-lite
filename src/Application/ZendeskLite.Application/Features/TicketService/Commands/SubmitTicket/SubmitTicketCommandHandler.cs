using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.TicketService.Commands.SubmitTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class SubmitTicketCommandHandler : IRequestHandler<SubmitTicketCommand, Result<Guid>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<SubmitTicketCommandHandler> _logger;

    public SubmitTicketCommandHandler(
        ITicketRepository ticketRepository,
        ILogger<SubmitTicketCommandHandler> logger,
        ICurrentUser currentUser,
        IMessagePublisher messagePublisher)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SubmitTicketCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure<Guid>(Error.Failure("Auth.Unauthorized", "User is not authenticated."));
        }

        _logger.LogInformation("Submitting ticket for customer: {CustomerId}", _currentUser.UserId);

        var ticket = new Ticket
        {
            Title = request.Title,
            RawDescription = request.Description,
            CustomerId = _currentUser.UserId!,
        };

        await _ticketRepository.AddAsync(ticket, ct);

        await _messagePublisher.PublishAsync(
            new TicketSubmittedEvent(ticket.Id),
            routingKey: "ticket.submitted",
            ct);

        _logger.LogInformation("Ticket {TicketId} saved and queued for background processing.", ticket.Id);

        return Result.Success(ticket.Id);
    }
}