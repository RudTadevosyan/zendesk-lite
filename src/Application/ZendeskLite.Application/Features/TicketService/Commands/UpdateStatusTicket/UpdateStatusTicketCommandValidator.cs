using FluentValidation;

namespace ZendeskLite.Application.Features.TicketService.Commands.UpdateStatusTicket;

public class UpdateStatusTicketCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    public UpdateStatusTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Please provide a reason for the status change.")
            .MaximumLength(1000);
    }
}