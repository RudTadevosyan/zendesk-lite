using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;
using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string RawDescription { get; set; } = string.Empty;
        public string? CleanedDescription { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.New;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketCategory Category { get; set; } = TicketCategory.General;

        public string? Comments { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? AgentId { get; set; }
    }
}
