using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.TicketService.Queries.GetAdminTicket;
using ZendeskLite.Domain.Common;

public class GetAdminTicketByIdQueryHandler : IRequestHandler<GetAdminTicketByIdQuery, Result<AdminTicketDto>>
{
    private readonly ITicketAuditRepository _ticketAuditRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<GetAdminTicketByIdQueryHandler> _logger;
    private readonly ICurrentUser _currentUser; 

    public GetAdminTicketByIdQueryHandler(
        ITicketAuditRepository ticketAuditRepository,
        ITicketRepository ticketRepository,
        ILogger<GetAdminTicketByIdQueryHandler> logger,
        ICurrentUser currentUser)
    {
        _ticketAuditRepository = ticketAuditRepository;
        _ticketRepository = ticketRepository;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminTicketDto>> Handle(GetAdminTicketByIdQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAgent)
        {
            _logger.LogWarning("Unauthorized access attempt to GetAdminTicketById by user {UserId}", _currentUser.UserId);
            return Result.Failure<AdminTicketDto>(Error.Validation("403", "Forbidden"));
        }

        _logger.LogInformation("Retrieving admin ticket {TicketId} for user {UserId}", request.Id, _currentUser.UserId);

        var ticket = await _ticketRepository.GetByIdAsync(request.Id, ct);
        if (ticket == null)
            return Result.Failure<AdminTicketDto>(Error.NotFound("404", "Ticket not found"));

        var logs = await _ticketAuditRepository.GetLogsByTicketIdAsync(request.Id, ct);

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