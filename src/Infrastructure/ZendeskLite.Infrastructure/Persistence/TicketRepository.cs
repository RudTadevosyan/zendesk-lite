using Microsoft.EntityFrameworkCore;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Infrastructure.Persistence;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;
    public TicketRepository(ApplicationDbContext context) => _context = context;

    public async Task AddAsync(Ticket ticket, CancellationToken ct)
    {
        await _context.Tickets.AddAsync(ticket, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket != null)
        {
            ticket.SoftDelete();
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task<PagedResult<Ticket>> GetFilteredAsync(TicketQueryParameters p, CancellationToken ct)
    {
        IQueryable<Ticket> query = _context.Tickets.Where(t => !t.IsDeleted);

        if (!string.IsNullOrEmpty(p.UserId))
            query = query.Where(t => t.CustomerId == p.UserId || t.AgentId == p.UserId);

        if (p.Status.HasValue)
            query = query.Where(t => t.Status == p.Status);

        if (p.Priority.HasValue)
            query = query.Where(t => t.Priority == p.Priority);

        int totalCount = await query.CountAsync(ct);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((p.Page - 1) * p.PageSize)
            .Take(p.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Ticket>(tickets, totalCount, p.Page, p.PageSize);
    }
}