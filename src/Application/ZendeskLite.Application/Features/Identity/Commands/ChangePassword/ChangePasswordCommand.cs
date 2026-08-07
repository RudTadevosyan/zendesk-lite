using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.Identity.Commands.ChangePassword
{
    public record ChangePasswordCommand(string CurrentUserId, string AccessToken, 
        string CurrentPassword, string NewPassoword) : IRequest<Result>;
}
