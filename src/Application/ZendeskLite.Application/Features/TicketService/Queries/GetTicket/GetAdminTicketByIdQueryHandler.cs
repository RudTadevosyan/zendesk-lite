using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetTicket
{
    public class GetAdminTicketByIdQueryHandler : IRequestHandler<GetAdminTicketByIdQuery, Result<AdminTicketDto>>
    {

        private readonly ITicketAuditRepository _ticketAuditRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<GetAdminTicketByIdQueryHandler> _logger;
        public GetAdminTicketByIdQueryHandler(ITicketAuditRepository ticketAuditRepository, 
            ITicketRepository ticketRepository,
            ILogger<GetAdminTicketByIdQueryHandler> logger)
        {
            _ticketAuditRepository = ticketAuditRepository;
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result<AdminTicketDto>> Handle(GetAdminTicketByIdQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Handling GetAdminTicketByIdQuery for Ticket ID: {TicketId} by Admin/Agent ID: {AdminAgentId}", request.Id, request.AdminAgentId);
            var ticket = await _ticketRepository.GetByIdAsync(request.Id, ct);
            if (ticket == null)
                return Result.Failure<AdminTicketDto>(Error.NotFound("404", "Ticket not found"));

            var logs = await _ticketAuditRepository.GetLogsByTicketIdAsync(request.Id, ct);

            _logger.LogInformation("Retrieved Ticket ID: {TicketId} with {LogCount} audit logs", request.Id, logs.Count);

            var adminDto = new AdminTicketDto(
                ticket.Id, ticket.Title, ticket.RawDescription, ticket.CleanedDescription,
                ticket.Status, ticket.Category, ticket.Comments, ticket.CreatedAt,
                ticket.LastModifiedAt, ticket.LastModifiedAt, 
                ticket.IsDeleted, ticket.Priority, ticket.CustomerId, ticket.AgentId,
                logs.Select(l => new TicketAuditLogDto(l.Action, l.ChangedByUserId, l.Notes, l.CreatedAt)).ToList()
            );

            return Result.Success(adminDto);
        }
    }
}
