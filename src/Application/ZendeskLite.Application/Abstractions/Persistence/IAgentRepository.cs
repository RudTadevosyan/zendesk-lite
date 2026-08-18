using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.DTOs;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.Abstractions.Persistence
{
    public interface IAgentRepository
    {
        Task<AppUser?> GetByIdAsync(string agentId, CancellationToken cancellationToken);

        Task<AppUser?> GetBestAvailableAgentAsync(TicketCategory category, CancellationToken cancellationToken);

        Task IncrementActiveLoadAsync(string agentId, CancellationToken cancellationToken);

        Task DecrementActiveLoadAsync(string agentId, CancellationToken cancellationToken);

        Task SetAvailabilityAsync(string agentId, bool isAvailable, CancellationToken cancellationToken);
        Task<IEnumerable<AppUser>> GetAllAgentsAsync(CancellationToken cancellationToken);
    }
}
