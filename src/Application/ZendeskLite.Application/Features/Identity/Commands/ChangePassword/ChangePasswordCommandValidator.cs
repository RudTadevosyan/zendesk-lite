using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.Identity.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {

        public ChangePasswordCommandValidator() 
        {
            RuleFor(x => x.CurrentPassword).NotEqual(x => x.NewPassoword).WithMessage("New password must be different from current password.");
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassoword).NotEmpty();
        }
    }
}
