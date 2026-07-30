using Microsoft.Extensions.Caching.Distributed;
using System.IdentityModel.Tokens.Jwt;

namespace ZendeskLite.API.Middlewares
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }   

        public async Task InvokeAsync(HttpContext context, IDistributedCache tokenService)
        {
            var authHandler = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHandler != null && authHandler.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHandler.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();

                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        var isBlacklisted = await tokenService.GetStringAsync($"blacklist:{jti}");
                        if (!string.IsNullOrEmpty(isBlacklisted))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Token is blacklisted.");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
