using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.Features.TicketService.Queries.ListTicket;
using ZendeskLite.Domain.Common;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PagedResult<BaseTicketDto>>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<GetTicketsQueryHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public GetTicketsQueryHandler(ITicketRepository ticketRepository, ILogger<GetTicketsQueryHandler> logger, ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<BaseTicketDto>>> Handle(GetTicketsQuery request, CancellationToken ct)
    {
        // Admins/Agents see everything, customers only see their own
        string? userIdFilter = _currentUser.IsAdminOrAgent ? null : _currentUser.UserId;

        _logger.LogInformation("Fetching tickets for User: {UserId}, Filtered by: {UserIdFilter}", _currentUser.UserId, userIdFilter);

        var parameters = new TicketQueryParameters(
            userIdFilter,
            request.Status,
            request.Priority,
            request.Page,
            request.PageSize
        );

        var result = await _ticketRepository.GetFilteredTicketsAsync(parameters, ct);

        var dto = new PagedResult<BaseTicketDto>(
            result.Items.Select(t => new BaseTicketDto(
                t.Id, t.Title, t.RawDescription, t.CleanedDescription,
                t.Status, t.Category, t.Comments, t.CreatedAt
            )).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize
        );

        return Result.Success(dto);
    }
}