using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.DTOs.Request.Identity
{
    public record RegisterRequestBody(string FirstName, string LastName, string Email, string Password, string ConfirmPassword);

}
