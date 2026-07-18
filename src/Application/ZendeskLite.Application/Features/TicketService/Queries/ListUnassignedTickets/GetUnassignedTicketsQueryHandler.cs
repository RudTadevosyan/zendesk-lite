using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.TicketService.Queries.ListUnassignedTickets;
using ZendeskLite.Domain.Common;

public class GetUnassignedTicketsQueryHandler : IRequestHandler<GetUnassignedTicketsQuery, Result<PagedResult<BaseTicketDto>>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<GetUnassignedTicketsQueryHandler> _logger;
    private readonly ICurrentUser _currentUser; 

    public GetUnassignedTicketsQueryHandler(
        ITicketRepository ticketRepository,
        ILogger<GetUnassignedTicketsQueryHandler> logger,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<BaseTicketDto>>> Handle(GetUnassignedTicketsQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsAdminOrAgent)
        {
            _logger.LogWarning("Unauthorized attempt to list unassigned tickets by user {UserId}", _currentUser.UserId);
            return Result.Failure<PagedResult<BaseTicketDto>>(Error.Validation("403", "Forbidden"));
        }

        _logger.LogInformation("Fetching unassigned tickets for page {Page} by Agent {AgentId}", request.Page, _currentUser.UserId);

        var pagedTickets = await _ticketRepository.GetUnassignedTicketsAsync(request.Page, request.PageSize, ct);

        var dtos = pagedTickets.Items.Select(t => new BaseTicketDto(
            t.Id, t.Title, t.RawDescription, t.CleanedDescription,
            t.Status, t.Category, t.Comments, t.CreatedAt
        )).ToList();

        return Result.Success(new PagedResult<BaseTicketDto>(
            dtos, pagedTickets.TotalCount, request.Page, request.PageSize));
    }
}