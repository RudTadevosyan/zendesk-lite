using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.TicketService.Commands.AssignTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketAuditRepository _auditRepository;
    private readonly IAgentRepository _agentRepository; 
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AssignTicketCommandHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public AssignTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketAuditRepository auditRepository,
        IAgentRepository agentRepository,
        IApplicationDbContext context,
        ILogger<AssignTicketCommandHandler> logger,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _auditRepository = auditRepository;
        _agentRepository = agentRepository;
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket == null) return Result.Failure(Error.NotFound("404", "Ticket not found"));

        if (!_currentUser.IsAdmin && request.TargetAgentId != _currentUser.UserId)
        {
            _logger.LogWarning("Agent {UserId} attempted to assign ticket to {TargetAgentId}", _currentUser.UserId, request.TargetAgentId);
            return Result.Failure(Error.Validation("403", "Agents can only assign tickets to themselves."));
        }

        // Capture previous agent ID to handle re-assignment/transfer counts correctly
        var previousAgentId = ticket.AgentId;
        var isNewAssignment = string.IsNullOrEmpty(previousAgentId);
        var isReassignment = !isNewAssignment && previousAgentId != request.TargetAgentId;

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            ticket.AgentId = request.TargetAgentId;
            ticket.UpdateLastModified();
            await _ticketRepository.UpdateAsync(ticket, ct);

            if (isNewAssignment)
            {
                await _agentRepository.IncrementActiveLoadAsync(request.TargetAgentId, ct);
            }
            else if (isReassignment)
            {
                // Decrement old agent and increment new agent
                await _agentRepository.DecrementActiveLoadAsync(previousAgentId!, ct);
                await _agentRepository.IncrementActiveLoadAsync(request.TargetAgentId, ct);
            }

            await _auditRepository.AddAuditLogAsync(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "Ticket Assigned",
                ChangedByUserId = _currentUser.UserId!,
                Notes = $"Ticket assigned to Agent {request.TargetAgentId}"
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Ticket {TicketId} assigned to {AgentId} by {User}", ticket.Id, request.TargetAgentId, _currentUser.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to assign ticket {TicketId}", request.TicketId);
            return Result.Failure(Error.Failure("500", "Assignment failed."));
        }
    }
}