using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZendeskLite.Application.Common.Extensions;
using ZendeskLite.Application.DTOs.Request.Ticket;
using ZendeskLite.Application.Features.TicketService.Commands.AddTicketComment;
using ZendeskLite.Application.Features.TicketService.Commands.AssignTicket;
using ZendeskLite.Application.Features.TicketService.Commands.SoftDeleteTicket;
using ZendeskLite.Application.Features.TicketService.Commands.SubmitTicket;
using ZendeskLite.Application.Features.TicketService.Commands.UpdateStatusTicket;
using ZendeskLite.Application.Features.TicketService.Queries.GetAdminTicket;
using ZendeskLite.Application.Features.TicketService.Queries.GetTicket;
using ZendeskLite.Application.Features.TicketService.Queries.ListMyTickets;
using ZendeskLite.Application.Features.TicketService.Queries.ListTickets;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tickets")]
    public class TicketController : ControllerBase
    {
        private readonly ISender _sender;

        public TicketController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTicketById(Guid id, CancellationToken ct)
        {
            var query = new GetTicketByIdQuery(id);
            var result = await _sender.Send(query, ct);

            return result.Match(
                onSuccess: ticket => Ok(ticket),
                onFailure: HandleError
            );
        }

        [HttpGet("/api/admin/tickets/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminTicketById(Guid id, CancellationToken ct)
        {
            var query = new GetAdminTicketByIdQuery(id);
            var result = await _sender.Send(query, ct);

            return result.Match(
                onSuccess: ticket => Ok(ticket),
                onFailure: HandleError
            );
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ListMyTickets(
            [FromQuery] TicketStatus? status,
            [FromQuery] TicketCategory? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var query = new ListMyTicketsQuery(status, category, page, pageSize);
            var result = await _sender.Send(query, ct);

            return result.Match(
                onSuccess: pagedResult => Ok(pagedResult),
                onFailure: HandleError
            );
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> ListTickets(
            [FromQuery] string? userId,
            [FromQuery] string? agentId,
            [FromQuery] TicketStatus? status,
            [FromQuery] TicketPriority? priority,
            [FromQuery] TicketCategory? category,
            [FromQuery] bool isAssigned = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var query = new ListTicketsQuery(userId, agentId, status, priority, category, isAssigned, page, pageSize);
            var result = await _sender.Send(query, ct);

            return result.Match(
                onSuccess: pagedResult => Ok(pagedResult),
                onFailure: HandleError
            );
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTicket([FromBody] SubmitTicketRequestBody requestBody, CancellationToken ct)
        {
            var command = new SubmitTicketCommand(requestBody.Title, requestBody.Description);
            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: ticketId => Ok(new { id = ticketId }),
                onFailure: HandleError
            );
        }

        [HttpPost("{ticketId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid ticketId, [FromBody] AddCommentRequestBody requestBody, CancellationToken ct)
        {
            var command = new AddCommentCommand(ticketId, requestBody.CommentText);
            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: () => Ok(),
                onFailure: HandleError
            );
        }

        [HttpPost("{ticketId:guid}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTicket(Guid ticketId, [FromBody] AssignTicketRequestBody requestBody, CancellationToken ct)
        {
            var command = new AssignTicketCommand(ticketId, requestBody.TargetAgentId);
            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: () => Ok(),
                onFailure: HandleError
            );
        }

        [HttpPatch("{ticketId:guid}/status")]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> UpdateTicketStatus(Guid ticketId, [FromBody] UpdateTicketStatusRequestBody requestBody, CancellationToken ct)
        {
            var command = new UpdateTicketStatusCommand(ticketId, requestBody.NewStatus, requestBody.Notes);
            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: () => Ok(),
                onFailure: HandleError
            );
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SoftDeleteTicket(Guid id, CancellationToken ct)
        {
            var command = new SoftDeleteTicketCommand(id);
            var result = await _sender.Send(command, ct);

            return result.Match(
                onSuccess: () => Ok(),
                onFailure: HandleError
            );
        }

        private IActionResult HandleError(Error error)
        {
            return error.Type switch
            {
                ErrorType.NotFound => NotFound(error),
                ErrorType.Validation => BadRequest(error),
                ErrorType.Conflict => Conflict(error),
                _ => BadRequest(error)
            };
        }
    }
}