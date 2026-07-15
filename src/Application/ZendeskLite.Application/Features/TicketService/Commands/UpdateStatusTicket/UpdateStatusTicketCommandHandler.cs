using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Features.TicketService.Commands.UpdateStatusTicket
{
    public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ITicketAuditRepository _auditRepository;
        private readonly ILogger<UpdateTicketStatusCommandHandler> _logger;
        private readonly IApplicationDbContext _context;

        public UpdateTicketStatusCommandHandler(
            ITicketRepository ticketRepository,
            ITicketAuditRepository auditRepository,
            IApplicationDbContext context,
            ILogger<UpdateTicketStatusCommandHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _auditRepository = auditRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateTicketStatusCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Handling UpdateTicketStatusCommand for TicketId: {TicketId}", request.TicketId);
            var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
            if (ticket == null)
                return Result.Failure(Error.NotFound("404", "Ticket not found"));

            if (ticket.AgentId != request.AgentId && !request.IsAdmin)
            {
                _logger.LogWarning("Agent {AgentId} tried to update ticket {TicketId} they are not assigned to.", request.AgentId, request.TicketId);
                return Result.Failure(Error.Validation("403", "You are not assigned to this ticket."));
            }

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {

                // need to add both at the same time to ensure consistency
                _logger.LogInformation("Updating status of TicketId: {TicketId} from {OldStatus} to {NewStatus}", ticket.Id, ticket.Status, request.NewStatus);
                ticket.Status = request.NewStatus;
                ticket.UpdateLastModified();
                await _ticketRepository.UpdateAsync(ticket, ct);

                var log = new TicketAuditLog
                {
                    TicketId = ticket.Id,
                    Action = $"Status changed to {request.NewStatus}",
                    ChangedByUserId = request.AgentId,
                    Notes = request.Notes
                };

                await _auditRepository.AddAuditLogAsync(log, ct);

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Transaction failed for TicketId: {TicketId}. Rolling back.", request.TicketId);
                return Result.Failure(Error.Failure("500", "An internal error occurred while updating the ticket status."));
            }
        }
    }
}
