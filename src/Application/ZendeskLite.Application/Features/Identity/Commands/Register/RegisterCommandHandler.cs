using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<TokenResponse>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<RegisterCommandHandler> _logger;
        private readonly UserManager<AppUser> _userManager;

        public RegisterCommandHandler(
            ITokenService tokenService,
            ILogger<RegisterCommandHandler> logger,
            UserManager<AppUser> userManager)
        {
            _tokenService = tokenService;
            _logger = logger;
            _userManager = userManager;
        }
        public async Task<Result<TokenResponse>> Handle(RegisterCommand request, CancellationToken ct)
        {
            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            // manual pre-validation before calling CreateAsync
            foreach (var validator in _userManager.PasswordValidators)
            {
                var validationResult = await validator.ValidateAsync(_userManager, user, request.Password);
                if (!validationResult.Succeeded)
                {
                    var error = validationResult.Errors.First();
                    return Result.Failure<TokenResponse>(Error.Failure(error.Code, error.Description));
                }
            }

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                return Result.Failure<TokenResponse>(Error.Conflict("Registration.Failed", errorMessages));
            }

            // Assign default role "Customer"
            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
            if (!roleResult.Succeeded)
            {
                // Optional: remove the created user to avoid a user with no role
                await _userManager.DeleteAsync(user);

                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to assign role to new user {Email}: {Errors}", user.Email, roleErrors);
                return Result.Failure<TokenResponse>(Error.Failure("Registration.RoleAssignmentFailed", roleErrors));
            }

            return await _tokenService.GenerateTokenAsync(user, ct);
        }
    }
}
