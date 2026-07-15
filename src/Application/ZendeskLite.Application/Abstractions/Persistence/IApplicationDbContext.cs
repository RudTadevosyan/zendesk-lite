using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketAuditLog> TicketAuditLogs { get; }
    DatabaseFacade Database { get; } 
    Task<int> SaveChangesAsync(CancellationToken ct);
}