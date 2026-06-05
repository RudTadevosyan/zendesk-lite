using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Infrastructure.Persistence;
using ZendeskLite.Infrastructure.Persistence.Seed;

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

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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