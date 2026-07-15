using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Application.Abstractions.Persistence
{
    public interface ITicketAuditRepository
    {
        Task<List<TicketAuditLog>> GetLogsByTicketIdAsync(Guid ticketId, CancellationToken ct);
        Task AddAuditLogAsync(TicketAuditLog log, CancellationToken ct);
    }
}
