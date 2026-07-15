using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.TicketService.Commands.AssignTicket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketAuditRepository _auditRepository;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AssignTicketCommandHandler> _logger;

    public AssignTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketAuditRepository auditRepository,
        IApplicationDbContext context,
        ILogger<AssignTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _auditRepository = auditRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignTicketCommand request, CancellationToken ct)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket == null) return Result.Failure(Error.NotFound("404", "Ticket not found"));

        // Business Rule: Agents can only assign to themselves
        if (!request.IsAdmin && request.TargetAgentId != request.RequestingUserId)
        {
            _logger.LogWarning("Agent {UserId} attempted to assign ticket to {TargetAgentId}", request.RequestingUserId, request.TargetAgentId);
            return Result.Failure(Error.Validation("403", "Agents can only assign tickets to themselves."));
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            ticket.AgentId = request.TargetAgentId;
            ticket.UpdateLastModified();
            await _ticketRepository.UpdateAsync(ticket, ct);

            await _auditRepository.AddAuditLogAsync(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "Ticket Assigned",
                ChangedByUserId = request.RequestingUserId,
                Notes = $"Ticket assigned to Agent {request.TargetAgentId}"
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Ticket {TicketId} assigned to {AgentId} by {User}", ticket.Id, request.TargetAgentId, request.RequestingUserId);
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