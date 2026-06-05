using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Identity
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
    }

}
