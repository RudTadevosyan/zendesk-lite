using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Domain.Entities
{
    public class TicketAuditLog : BaseEntity
    {
        public Guid TicketId { get; set; }

        // Last action performed on the tickets
        public string Action { get; set; } = string.Empty;
        public string ChangedByUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
