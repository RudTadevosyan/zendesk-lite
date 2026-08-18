using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.DTOs
{
    public record AgentWorkloadDto(
        string AgentId,
        string FirstName,
        string LastName,
        string Email,
        TicketCategory? AgentSpecialty,
        bool IsAvailable,
        int ActiveTicketCount
    );
}
