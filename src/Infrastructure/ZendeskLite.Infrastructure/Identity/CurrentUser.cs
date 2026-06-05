using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ZendeskLite.Application.Identity;

namespace ZendeskLite.Infrastructure.Identity
{
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
        public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
