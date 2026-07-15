using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetTicket
{
    public class GetAdminTicketByIdQueryValidator : AbstractValidator<GetAdminTicketByIdQuery>
    {
        public GetAdminTicketByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Ticket ID is required.");

            RuleFor(x => x.AdminAgentId)
                .NotEmpty().WithMessage("Admin/Agent identification is required.");
        }
    }
}
