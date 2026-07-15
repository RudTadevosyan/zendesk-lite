using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Infrastructure.Persistence
{
    public class TicketAuditRepository : ITicketAuditRepository
    {

        private readonly ApplicationDbContext _context;
        public TicketAuditRepository(ApplicationDbContext context) => _context = context;

        public async Task<List<TicketAuditLog>> GetLogsByTicketIdAsync(Guid ticketId, CancellationToken ct)
        {
            return await _context.TicketAuditLogs
                .Where(log => log.TicketId == ticketId)
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task AddAuditLogAsync(TicketAuditLog log, CancellationToken ct)
        {
            await _context.TicketAuditLogs.AddAsync(log, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
