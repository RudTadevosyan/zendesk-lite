using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Revoke
{
    public class RevokeCommandHandler : IRequestHandler<RevokeCommand, Result>
    {
        private readonly ILogger<RevokeCommandHandler> _logger;
        private readonly ITokenService _tokenService;

        public RevokeCommandHandler(ITokenService tokenService, ILogger<RevokeCommandHandler> logger)
        {
            _tokenService = tokenService;
            logger = _logger;
        }

        public async Task<Result> Handle(RevokeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to revoke refresh token");   
            return await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        }
    }
}
