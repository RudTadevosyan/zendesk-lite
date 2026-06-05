using Microsoft.AspNetCore.Identity;
using ZendeskLite.Domain.Enums;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // for agents
    public TicketCategory? AgentSpecialty { get; set; }
    public int ActiveTicketCount { get; set; } = 0;
}