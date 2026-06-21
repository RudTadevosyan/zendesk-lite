using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ZendeskLite.Application.Abstractions.Common.Interfaces;

namespace ZendeskLite.Infrastructure.Identity
{
    public sealed class CurrentUser : ICurrentUser
    {
        private readonly ClaimsPrincipal? _user;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _user = httpContextAccessor.HttpContext?.User;
        }

        public string? UserId => _user?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Email => _user?.FindFirstValue(ClaimTypes.Email);

        public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
    }
}
