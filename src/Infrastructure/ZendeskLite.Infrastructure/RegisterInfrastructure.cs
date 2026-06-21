using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Infrastructure.Identity;
using ZendeskLite.Infrastructure.Persistence;
using ZendeskLite.Infrastructure.Services;

namespace ZendeskLite.Infrastructure
{
    public static class RegisterInfrastructure
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // Register Identity
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;
            })
                    .AddEntityFrameworkStores<ApplicationDbContext>();

            // Register Redis Cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config.GetConnectionString("redis");
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
