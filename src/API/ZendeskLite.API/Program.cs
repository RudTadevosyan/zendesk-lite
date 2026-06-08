using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using ZendeskLite.Application;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Infrastructure;
using ZendeskLite.Infrastructure.Persistence;

namespace ZendeskLite.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Build the WebApplicationBuilder using Aspire defaults
        var builder = WebApplication.CreateBuilder(args);

        // Setup Serilog using the builder's configuration
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

        // Aspire Service Defaults
        builder.AddServiceDefaults();

        // Register Database and Identity
        builder.AddNpgsqlDbContext<ApplicationDbContext>("zendeskdb");

        builder.Services.AddDataProtection(); // Required for Identity tokens
        builder.Services.AddIdentityCore<AppUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Register Redis Cache
        builder.AddRedisClient("redis");
        builder.Services.AddStackExchangeRedisCache(options => {
            options.Configuration = builder.Configuration.GetConnectionString("redis");
        });

        // DI
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddEndpointsApiExplorer();

        // Authentication & JWT Setup
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });


        // Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
            });
        });


        builder.Services.AddControllers();

        var app = builder.Build();

        // 5. Middleware Pipeline
        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        // Run Migrations & Seeding AFTER the host starts
        app.Lifetime.ApplicationStarted.Register(async () =>
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                Log.Information("Applying database migrations...");
                var context = services.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();

                Log.Information("Seeding database...");
                await DatabaseSeeder.SeedAsync(services);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred during database migration/seeding.");
            }
        });

        await app.RunAsync();
    }
}