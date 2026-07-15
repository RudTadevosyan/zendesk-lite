using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.TicketService.Queries.GetTicket
{
    public class GetTicketByIdQueryValidator : AbstractValidator<GetTicketByIdQuery>
    {
        public GetTicketByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Ticket Id is required.");
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer Id is required.");
        }
    }
}
