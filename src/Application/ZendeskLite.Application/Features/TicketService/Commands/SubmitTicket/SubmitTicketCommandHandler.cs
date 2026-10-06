using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Events;
using ZendeskLite.Application.Features.TicketService.Commands.SubmitTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class SubmitTicketCommandHandler : IRequestHandler<SubmitTicketCommand, Result<Guid>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SubmitTicketCommandHandler> _logger;

    public SubmitTicketCommandHandler(
        ITicketRepository ticketRepository,
        ILogger<SubmitTicketCommandHandler> logger,
        ICurrentUser currentUser,
         IApplicationDbContext dbContext)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _dbContext = dbContext;
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

        var @event = new TicketSubmittedEvent(ticket.Id);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TicketSubmittedEvent).Name!,
            RoutingKey = "ticket.submitted",
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
            Processed = false,
            Attempts = 0
        };


        await _ticketRepository.AddAsync(ticket, ct);
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, ct);

        // save after adding both ticket and outbox message to ensure atomicity
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Ticket {TicketId} saved and queued for background processing.", ticket.Id);

        return Result.Success(ticket.Id);
    }
}