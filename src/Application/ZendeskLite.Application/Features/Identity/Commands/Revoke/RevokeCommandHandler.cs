using MediatR;
using Microsoft.Extensions.Logging;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Features.Identity.Commands.Revoke;
using ZendeskLite.Domain.Common;

public class RevokeCommandHandler : IRequestHandler<RevokeCommand, Result>
{
    private readonly ILogger<RevokeCommandHandler> _logger;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUser _currentUser;

    public RevokeCommandHandler(ITokenService tokenService, ILogger<RevokeCommandHandler> logger, ICurrentUser currentUser)
    {
        _tokenService = tokenService;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result.Failure(Error.Failure("Auth.Unauthorized", "You must be logged in."));
        }

        _logger.LogInformation("User {UserId} is attempting to revoke a token.", _currentUser.UserId);

        return await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, _currentUser.UserId!, ct);
    }
}