using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.TicketService.Commands
{
    public class SubmitTicketCommandValidator : AbstractValidator<SubmitTicketCommand>
    {
        public SubmitTicketCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer context is missing.");
        }

    }
}
