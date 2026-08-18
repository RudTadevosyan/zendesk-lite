using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Application.Abstractions.Common.Interfaces;
using ZendeskLite.Application.Abstractions.Persistence;
using ZendeskLite.Application.Features.AgentService.Command.ChangeAgentAvailability;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.AgentService.Command.ToggleAgentAvailability
{
    public class ChangeAgentAvailabilityCommandHandler : IRequestHandler<ChangeAgentAvailabilityCommand, Result>
    {
        private readonly IAgentRepository _agentRepository;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ChangeAgentAvailabilityCommandHandler> _logger;

        public ChangeAgentAvailabilityCommandHandler(IAgentRepository agentRepository, ICurrentUser currentUser,
            ILogger<ChangeAgentAvailabilityCommandHandler> logger)
        {
            _agentRepository = agentRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result> Handle(ChangeAgentAvailabilityCommand request, CancellationToken ct)
        {

            _logger.LogInformation("User {UserId} is attempting to change availability for agent {TargetAgentId} to {IsAvailable}.",
                _currentUser.UserId, request.TargetAgentId, request.IsAvailable);

            if (string.IsNullOrEmpty(_currentUser.UserId))
            {
                return Result.Failure(Error.Validation("401", "Unauthorized user context."));
            }

            var agentToUpdateId = request.TargetAgentId;
            bool isUpdatingOther = !string.IsNullOrEmpty(agentToUpdateId) && agentToUpdateId != _currentUser.UserId;

            // chceck if the current user is trying to update another agent's availability without admin privileges
            if (isUpdatingOther && !_currentUser.IsAdmin)
            {
                _logger.LogWarning("User {UserId} attempted to change availability for agent {TargetAgentId} without admin privileges.",
                    _currentUser.UserId, agentToUpdateId);
                return Result.Failure(Error.Validation("403", "You are not authorized to change other agents' availability."));
            }

            if (string.IsNullOrEmpty(agentToUpdateId))
            {
                agentToUpdateId = _currentUser.UserId;
            }

            await _agentRepository.SetAvailabilityAsync(agentToUpdateId, request.IsAvailable, ct);

            _logger.LogInformation("Agent {AgentId} availability status changed to: {IsAvailable} by user {ChangedBy}",
                agentToUpdateId, request.IsAvailable, _currentUser.UserId);

            return Result.Success();
        }
    }
}
