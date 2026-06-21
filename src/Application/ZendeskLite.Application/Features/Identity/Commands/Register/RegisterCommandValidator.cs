using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Features.Identity.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);

            RuleFor(x => x.Email).NotEmpty().EmailAddress();

            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
        }
    }
}
