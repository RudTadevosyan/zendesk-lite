using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = null!;

        public string RoutingKey { get; set; } = null!;

        public string Payload { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool Processed { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int Attempts { get; set; }
    }
}
