using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Application.DTOs.Request.Ticket;
using ZendeskLite.Application.DTOs.Response;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Queries.ListTickets
{
    public class ListTicketsQueryHandler : IRequestHandler<ListTicketsQuery, Result<PagedResult<BaseTicketDto>>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ListTicketsQueryHandler> _logger;

        public ListTicketsQueryHandler(
            ITicketRepository ticketRepository,
            ICurrentUser currentUser,
            ILogger<ListTicketsQueryHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<PagedResult<BaseTicketDto>>> Handle(ListTicketsQuery request, CancellationToken cancellationToken)
        {

            _logger.LogInformation(
                "Admin/Agent {StaffUserId} is listing tickets. Filters -> CustomerId: {CustomerId}, AgentId: {AgentId}, Status: {Status}, Priority: {Priority}, Category: {Category}, IsAssigned: {IsAssigned}",
                _currentUser.UserId, request.UserId, request.AgentId, request.Status, request.Priority, request.Category, request.IsAssigned);

            int pageNumber = request.PageNumber;
            int pageSize = request.PageSize;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100;

            var parameters = new TicketQueryParameters(
                UserId: request.UserId,
                AgentId: request.AgentId,
                Status: request.Status,
                Priority: request.Priority,
                Category: request.Category,
                IsAssigned: request.IsAssigned,
                PageNumber: pageNumber,
                PageSize: pageSize
            );

            var result = await _ticketRepository.GetFilteredTicketsAsync(parameters, cancellationToken);

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
