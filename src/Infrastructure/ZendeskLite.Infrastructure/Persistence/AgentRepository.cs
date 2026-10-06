using Microsoft.EntityFrameworkCore;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Domain.Common;
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

        // helper query for agent filtering
        private IQueryable<AppUser> QueryAgents()
        {
            return _context.Users
                .Where(user => _context.UserRoles
                    .Where(ur => _context.Roles
                        .Any(r => r.Id == ur.RoleId && r.Name == "Agent")
                    )
                    .Any(ur => ur.UserId == user.Id)
                );
        }

        public async Task<AppUser?> GetByIdAgentAsync(string agentId, CancellationToken cancellationToken)
        {
            return await QueryAgents()
                .FirstOrDefaultAsync(u => u.Id == agentId, cancellationToken);
        }

        public async Task<AppUser?> GetBestAvailableAgentAsync(TicketCategory category, CancellationToken cancellationToken)
        {
            return await QueryAgents()
                .Where(u => u.AgentSpecialty == category && u.IsAvailable)
                .OrderBy(u => u.ActiveTicketCount)
                .ThenBy(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task IncrementActiveLoadAsync(string agentId, CancellationToken cancellationToken)
        {
            // Performs an atomic database-level increment.
            var affectedRows = await _context.Users
                .Where(a => a.Id == agentId && a.IsAvailable)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.ActiveTicketCount, u => u.ActiveTicketCount + 1), cancellationToken);

            if (affectedRows == 0)
            {
                throw new InvalidOperationException($"Agent {agentId} was not found or is no longer available.");
            }
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

        public async Task<PagedResult<AppUser>> GetAllAgentsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var query = QueryAgents();

            var totalCount = await query.CountAsync(cancellationToken);

            var agents = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AppUser>(agents, totalCount, pageNumber, pageSize);
        }
    }
}