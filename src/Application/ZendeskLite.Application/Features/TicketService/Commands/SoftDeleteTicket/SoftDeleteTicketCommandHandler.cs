using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Commands.SoftDeleteTicket
{
    public class SoftDeleteTicketCommandHandler : IRequestHandler<SoftDeleteTicketCommand, Result>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ILogger<SoftDeleteTicketCommandHandler> _logger;

        public SoftDeleteTicketCommandHandler(
            ITicketRepository ticketRepository,
            ILogger<SoftDeleteTicketCommandHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result> Handle(SoftDeleteTicketCommand request, CancellationToken ct)
        {
            var ticket = await _ticketRepository.GetByIdAsync(request.Id, ct);
            if (ticket == null)
            {
                return Result.Failure(Error.NotFound("Ticket.NotFound", $"Ticket with ID {request.Id} was not found."));
            }

            await _ticketRepository.SoftDeleteAsync(request.Id, ct);

            _logger.LogInformation("Ticket with ID {TicketId} was successfully soft-deleted.", request.Id);

            return Result.Success();
        }
    }
}