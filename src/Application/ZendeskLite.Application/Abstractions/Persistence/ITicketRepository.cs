using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ZendeskLite.Application.DTOs.Request.Ticket;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.Abstractions.Persistence
{
    public interface ITicketRepository
    {
        Task AddAsync(Ticket ticket, CancellationToken ct);
        Task UpdateAsync(Ticket ticket, CancellationToken ct);
        Task SoftDeleteAsync(Guid id, CancellationToken ct);
        Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Ticket?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct);
        Task<PagedResult<Ticket>> GetFilteredTicketsAsync(TicketQueryParameters parameters, CancellationToken ct);
    }
}
