using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(ITokenService tokenService, 
            UserManager<AppUser> userManager, ILogger<ChangePasswordCommandHandler> logger)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to change password for user: {UserId}", request.CurrentUserId);

            // find user
            var user = await _userManager.FindByIdAsync(request.CurrentUserId);
            if (user == null) return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

            // change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassoword);
            if (!changePasswordResult.Succeeded)
            {
                var errorMessages = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                return Result.Failure(Error.Validation("Failed to change password", errorMessages));
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", user.Id);

            // update security stamp to invalidate existing tokens
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("Security stamp updated for user: {UserId}", user.Id);
            /*
             If you only rely on the Security Stamp, the very next request they make (or a concurrent request fired a split second later) 
            might slip through before the database query catches up, or it forces an extra database lookup.
            */
            _logger.LogInformation("Revoking access token for user: {UserId}", user.Id);
            await _tokenService.RevokeAccessTokenAsync(request.AccessToken, cancellationToken);

            _logger.LogInformation("Revoking all sessions for user: {UserId}", user.Id);
            await _tokenService.RevokeAllUserRefreshTokensAsync(user.Id, cancellationToken);

            _logger.LogInformation("Password changed successfully and all sessions revoked for user: {UserId}", user.Id);
            return Result.Success();


        }
    }
}
