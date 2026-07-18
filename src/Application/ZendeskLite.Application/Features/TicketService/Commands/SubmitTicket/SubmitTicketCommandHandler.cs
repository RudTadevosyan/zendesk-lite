using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.TicketService.Commands.SubmitTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class SubmitTicketCommandHandler : IRequestHandler<SubmitTicketCommand, Result<Guid>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SubmitTicketCommandHandler> _logger;

    public SubmitTicketCommandHandler(
        ITicketRepository ticketRepository,
        ILogger<SubmitTicketCommandHandler> logger,
        ICurrentUser currentUser) 
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _currentUser = currentUser;
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

        _logger.LogInformation("Ticket submitted successfully with ID: {TicketId}", ticket.Id);
        return Result.Success(ticket.Id);
    }
}