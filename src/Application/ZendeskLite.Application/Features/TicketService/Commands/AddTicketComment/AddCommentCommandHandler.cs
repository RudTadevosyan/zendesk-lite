using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.TicketService.Commands.AddTicketComment;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketAuditRepository _auditRepository;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AddCommentCommandHandler> _logger;
    private readonly ICurrentUser _currentUser; 

    public AddCommentCommandHandler(
        ITicketRepository ticketRepository,
        ITicketAuditRepository auditRepository,
        IApplicationDbContext context,
        ILogger<AddCommentCommandHandler> logger,
        ICurrentUser currentUser) 
    {
        _ticketRepository = ticketRepository;
        _auditRepository = auditRepository;
        _context = context;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(AddCommentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var isAdminOrAgent = _currentUser.IsAdminOrAgent; 

        _logger.LogInformation("Attempting to add comment to TicketId: {TicketId} by User: {UserId}", request.TicketId, userId);

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket == null)
        {
            _logger.LogWarning("AddComment failed: Ticket {TicketId} not found.", request.TicketId);
            return Result.Failure(Error.NotFound("404", "Ticket not found"));
        }

        if (ticket.CustomerId != userId && !isAdminOrAgent)
        {
            _logger.LogWarning("Unauthorized comment attempt on Ticket {TicketId} by User {UserId}", request.TicketId, userId);
            return Result.Failure(Error.Validation("403", "You are not authorized to comment on this ticket."));
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            ticket.Comments = request.CommentText;
            ticket.UpdateLastModified();
            await _ticketRepository.UpdateAsync(ticket, ct);

            await _auditRepository.AddAuditLogAsync(new TicketAuditLog
            {
                TicketId = ticket.Id,
                Action = "Comment Added",
                ChangedByUserId = userId!, // Safe to use because we checked Auth status
                Notes = "New comment added to ticket."
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Successfully added comment to Ticket {TicketId} by User {UserId}", ticket.Id, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding a comment to Ticket {TicketId} by User {UserId}", request.TicketId, userId);
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Failure("500", "An internal error occurred."));
        }
    }
}