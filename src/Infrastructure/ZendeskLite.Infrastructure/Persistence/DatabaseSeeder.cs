using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        //  Resilient Migration Loop ---
        int maxRetries = 6;
        int delaySeconds = 2;
        bool migrationSucceeded = false;

        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                Log.Information("Attempting to verify database connection and apply migrations (Attempt {Retry}/{MaxRetries})...", retry, maxRetries);
                await context.Database.MigrateAsync();

                migrationSucceeded = true;
                Log.Information("Database migrations applied successfully.");
                break;
            }
            catch (Npgsql.NpgsqlException ex) when (ex.InnerException is EndOfStreamException || ex.Message.Contains("does not exist"))
            {
                Log.Warning("PostgreSQL is still establishing the 'zendeskdb' catalog. Waiting {Delay} seconds...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "A non-transient error occurred while applying migrations.");
                break; // Stop retrying for fatal system/syntax exceptions
            }
        }

        if (!migrationSucceeded)
        {
            Log.Error("Database migration failed. Skipping data seeding.");
            return;
        }

        // Data Seeding Execution
        try
        {
            Log.Information("Seeding default security roles and mock data...");
            await SeedRolesAsync(roleManager);
            await SeedAgentsAsync(userManager);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database tables.");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Admin", "Agent", "Customer"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                Log.Information("Created role: {Role}", roleName);
            }
        }
    }

    private static async Task SeedAgentsAsync(UserManager<AppUser> userManager)
    {
        // Tech Support Agent
        var techEmail = "agent.tech@zendesk.com";
        if (await userManager.FindByEmailAsync(techEmail) == null)
        {
            var techAgent = new AppUser
            {
                UserName = techEmail,
                Email = techEmail,
                FirstName = "Jane",
                LastName = "Tech",
                AgentSpecialty = TicketCategory.TechnicalSupport,
                ActiveTicketCount = 0,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(techAgent, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(techAgent, "Agent");
                Log.Information("Seeded Technical Support Agent: {Email}", techEmail);
            }
        }

        // Billing Agent
        var billingEmail = "agent.billing@zendesk.com";
        if (await userManager.FindByEmailAsync(billingEmail) == null)
        {
            var billingAgent = new AppUser
            {
                UserName = billingEmail,
                Email = billingEmail,
                FirstName = "John",
                LastName = "Billing",
                AgentSpecialty = TicketCategory.Billing,
                ActiveTicketCount = 0,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(billingAgent, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(billingAgent, "Agent");
                Log.Information("Seeded Billing Agent: {Email}", billingEmail);
            }
        }
    }
}