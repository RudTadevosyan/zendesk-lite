using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.Abstractions.Persistence
{
    public interface ITicketRepository
    {
        // Commands
        Task AddAsync(Ticket ticket, CancellationToken ct);
        Task UpdateAsync(Ticket ticket, CancellationToken ct);
        Task SoftDeleteAsync(Guid id, CancellationToken ct);

        // Queries
        Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<PagedResult<Ticket>> GetFilteredAsync(TicketQueryParameters parameters, CancellationToken ct);
    }

    public record TicketQueryParameters(string? UserId, TicketStatus? Status, TicketPriority? Priority,
        int Page = 1, int PageSize = 10);
}
