using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {

        private readonly ITokenService _tokenService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(ITokenService tokenService, ILogger<LogoutCommandHandler> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            
        }
    }
}
