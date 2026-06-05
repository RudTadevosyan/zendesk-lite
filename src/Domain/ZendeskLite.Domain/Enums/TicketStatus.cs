using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Domain.Enums
{
    public enum TicketStatus
    {
        New,          // new submission, waiting in queue
        Assigned,     // background worker processed it and assigned an agent
        UnderReview,  // agent is working on the issue
        Resolved,     // agent fixed the problem
        Archived      // soft-deleted
    }
}
