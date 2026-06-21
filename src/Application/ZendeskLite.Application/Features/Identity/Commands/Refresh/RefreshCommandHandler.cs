using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Refresh
{
    public class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<TokenResponse>>
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<RefreshCommandHandler> _logger;

        public RefreshCommandHandler(ITokenService tokenService, ILogger<RefreshCommandHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result<TokenResponse>> Handle(RefreshCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Attempting to refresh token with refresh token");
            return await _tokenService.RefreshTokenAsync(request.RefreshToken, ct);
        }
    
    }
}
