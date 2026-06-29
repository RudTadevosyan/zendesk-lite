using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Commands
{
    public class SubmitTicketCommandHandler : IRequestHandler<SubmitTicketCommand, Result<Guid>>
    {
        private readonly ILogger<SubmitTicketCommandHandler> _logger;
        private readonly ITicketRepository _ticketRepository;

        public SubmitTicketCommandHandler(ITicketRepository ticketRepository, ILogger<SubmitTicketCommandHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(SubmitTicketCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Submitting ticket for customer: {CustomerId}", request.CustomerId);
            var ticket = new Domain.Entities.Ticket
            {
                Title = request.Title,
                RawDescription = request.Description,
                CustomerId = request.CustomerId,
            };

            await _ticketRepository.AddAsync(ticket, cancellationToken);

            _logger.LogInformation("Ticket submitted successfully with ID: {TicketId}", ticket.Id);
            return Result.Success(ticket.Id);
        }
    }
}
