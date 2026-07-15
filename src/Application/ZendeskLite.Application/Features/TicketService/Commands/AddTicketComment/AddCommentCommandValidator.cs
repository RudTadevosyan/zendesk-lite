using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.TicketService.Commands.AddTicketComment
{
    public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
    {
        public AddCommentCommandValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.CommentText)
                .NotEmpty().WithMessage("Comment cannot be empty.")
                .MaximumLength(2000);
        }
    }
}
