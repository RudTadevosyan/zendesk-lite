using FluentValidation;

namespace ZendeskLite.Application.Features.TicketService.Commands.AssignTicket;

public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty().WithMessage("Ticket ID is required.");
        RuleFor(x => x.TargetAgentId).NotEmpty().WithMessage("An agent must be selected for assignment.");
    }
}