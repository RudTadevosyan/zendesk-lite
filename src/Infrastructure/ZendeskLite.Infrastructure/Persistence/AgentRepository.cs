using Microsoft.EntityFrameworkCore;
using Pipelines.Sockets.Unofficial.Arenas;
using System.Threading;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Entities;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Infrastructure.Persistence
{
    public class AgentRepository : IAgentRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetByIdAsync(string agentId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == agentId, cancellationToken);
        }

        public async Task<AppUser?> GetBestAvailableAgentAsync(TicketCategory category, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Where(u => u.AgentSpecialty == category && u.IsAvailable)
                .OrderBy(u => u.ActiveTicketCount)
                .FirstOrDefaultAsync(cancellationToken);
        }
        public async Task IncrementActiveLoadAsync(string agentId, CancellationToken cancellationToken)
        {
            // Performs an atomic database-level increment
            // completely thread-safe against race conditions across multiple workers.
            await _context.Users
                .Where(a => a.Id == agentId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTicketCount, u => u.ActiveTicketCount + 1), cancellationToken);
            // with this we don't need to call _context.SaveChangesAsync()
            // because ExecuteUpdateAsync handles it internally
        }
        public async Task DecrementActiveLoadAsync(string agentId, CancellationToken cancellationToken)
        {
            await _context.Users
                .Where(a => a.Id == agentId && a.ActiveTicketCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTicketCount, u => u.ActiveTicketCount - 1), cancellationToken);
        }

        public async Task SetAvailabilityAsync(string agentId, bool isAvailable, CancellationToken cancellationToken)
        {
            await _context.Users
                .Where(a => a.Id == agentId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsAvailable, isAvailable), cancellationToken);
        }

    }
}