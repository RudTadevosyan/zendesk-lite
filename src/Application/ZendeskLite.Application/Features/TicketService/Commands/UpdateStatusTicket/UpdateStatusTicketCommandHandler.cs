using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
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

        var oldStatus = ticket.Status;
        var newStatus = request.NewStatus;

        // Closing transition: moving from active/open to Resolved or Archived
        bool isTransitioningToClosed =
            (newStatus == TicketStatus.Resolved || newStatus == TicketStatus.Archived) &&
            (oldStatus != TicketStatus.Resolved && oldStatus != TicketStatus.Archived);

        // Re-opening transition: moving from Resolved back to an active/open status
        bool isTransitioningFromResolvedToActive =
            (oldStatus == TicketStatus.Resolved) &&
            (newStatus != TicketStatus.Resolved && newStatus != TicketStatus.Archived);

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    ticket.Status = newStatus;
                    ticket.UpdateLastModified();
                    await _ticketRepository.UpdateAsync(ticket, ct);

                    if (!string.IsNullOrEmpty(ticket.AgentId))
                    {
                        if (isTransitioningToClosed)
                        {
                            await _agentRepository.DecrementActiveLoadAsync(ticket.AgentId, ct);
                            _logger.LogInformation("Agent {AgentId} active load decremented because ticket {TicketId} closed (status: {Status})",
                                ticket.AgentId, ticket.Id, newStatus);
                        }
                        else if (isTransitioningFromResolvedToActive)
                        {
                            await _agentRepository.IncrementActiveLoadAsync(ticket.AgentId, ct);
                            _logger.LogInformation("Agent {AgentId} active load incremented because resolved ticket {TicketId} was re-opened (status: {Status})",
                                ticket.AgentId, ticket.Id, newStatus);
                        }
                    }

                    await _auditRepository.AddAuditLogAsync(new TicketAuditLog
                    {
                        TicketId = ticket.Id,
                        Action = $"Status changed from {oldStatus} to {newStatus}",
                        ChangedByUserId = _currentUser.UserId!,
                        Notes = request.Notes
                    }, ct);

                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    _logger.LogInformation("Ticket {TicketId} status updated to {Status} by {UserId}", ticket.Id, newStatus, _currentUser.UserId);
                    return Result.Success();
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for TicketId: {TicketId}.", request.TicketId);
            return Result.Failure(Error.Failure("500", "An internal error occurred."));
        }
    }
}