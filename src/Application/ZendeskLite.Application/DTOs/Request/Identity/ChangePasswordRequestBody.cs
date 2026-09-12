using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs.Request.Identity
{
    public record ChangePasswordRequestBody(string CurrentPassword, string NewPassword);
}
