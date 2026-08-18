using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.TicketService.Commands.UpdateStatusTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Domain.Enums;

public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketAuditRepository _auditRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser; 
    private readonly ILogger<UpdateTicketStatusCommandHandler> _logger;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository,
        ITicketAuditRepository auditRepository,
        IApplicationDbContext context,
        IAgentRepository agentRepository,
        ILogger<UpdateTicketStatusCommandHandler> logger,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _auditRepository = auditRepository;
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
        _agentRepository = agentRepository;
    }

    public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket == null)
            return Result.Failure(Error.NotFound("404", "Ticket not found"));

        if (ticket.AgentId != _currentUser.UserId && !_currentUser.IsAdmin)
        {
            _logger.LogWarning("User {UserId} tried to update ticket {TicketId} they are not assigned to.", _currentUser.UserId, request.TicketId);
            return Result.Failure(Error.Validation("403", "You are not authorized to update this ticket."));
        }

        // See if we closing the ticket (Resolved or Archived) and if the current status is not already closed
        bool isTransitioningToClosed =
            (request.NewStatus == TicketStatus.Resolved || request.NewStatus == TicketStatus.Archived) &&
            (ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Archived);

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            ticket.Status = request.NewStatus;
            ticket.UpdateLastModified();
            await _ticketRepository.UpdateAsync(ticket, ct);

            if (isTransitioningToClosed && !string.IsNullOrEmpty(ticket.AgentId))
            {
                await _agentRepository.DecrementActiveLoadAsync(ticket.AgentId, ct);
                _logger.LogInformation("Agent {AgentId} active load decremented because ticket {TicketId} status changed to {Status}",
                    ticket.AgentId, ticket.Id, request.NewStatus);
            }

            await _auditRepository.AddAuditLogAsync(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = $"Status changed to {request.NewStatus}",
                ChangedByUserId = _currentUser.UserId!,
                Notes = request.Notes
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Ticket {TicketId} status updated to {Status} by {UserId}", ticket.Id, request.NewStatus, _currentUser.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Transaction failed for TicketId: {TicketId}.", request.TicketId);
            return Result.Failure(Error.Failure("500", "An internal error occurred."));
        }
    }
}