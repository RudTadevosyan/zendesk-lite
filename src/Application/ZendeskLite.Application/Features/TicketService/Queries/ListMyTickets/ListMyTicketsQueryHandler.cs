using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.DTOs.Request.Ticket;
using ZendeskLite.Application.DTOs.Response;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListMyTickets
{
    public class ListMyTicketsQueryHandler : IRequestHandler<ListMyTicketsQuery, Result<PagedResult<BaseTicketDto>>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ListMyTicketsQueryHandler> _logger;

        public ListMyTicketsQueryHandler(
            ITicketRepository ticketRepository,
            ICurrentUser currentUser,
            ILogger<ListMyTicketsQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<PagedResult<BaseTicketDto>>> Handle(ListMyTicketsQuery request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                _logger.LogWarning("Unauthorized attempt to list personal tickets due to missing user context.");
                return Result.Failure<PagedResult<BaseTicketDto>>(Error.Validation("401", "Unauthorized user context."));
            }

            _logger.LogInformation("User {UserId} is listing their tickets with Status: {Status}, Category: {Category}",
                _currentUser.UserId, request.Status, request.Category);

            int pageNumber = request.PageNumber;
            int pageSize = request.PageSize;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var parameters = new TicketQueryParameters(
                            UserId: _currentUser.UserId,
                            AgentId: null,
                            Status: request.Status,
                            Priority: null,
                            Category: request.Category,
                            PageNumber: pageNumber,
                            PageSize: pageSize
                        );

            var result = await _ticketRepository.GetFilteredTicketsAsync(parameters, ct);

            var dtos = result.Items.Select(t => new BaseTicketDto(
                t.Id, t.Title, t.RawDescription, t.CleanedDescription,
                t.Status, t.Category, t.Comments, t.CreatedAt
            )).ToList();

            var pagedResult = new PagedResult<BaseTicketDto>(
                dtos,
                result.TotalCount,
                result.Page,
                result.PageSize
            );

            return Result.Success(pagedResult);
        }
    }
}