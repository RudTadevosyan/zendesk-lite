using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.TicketService.Commands.SoftDeleteTicket
{
    public class SoftDeleteTicketCommandValidator : AbstractValidator<SoftDeleteTicketCommand>
    {
        public SoftDeleteTicketCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Ticket ID is required.");
        }
    }
}
