using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZendeskLite.Domain.Common;

namespace ZendeskLite.Application.Features.TicketService.Commands.AddTicketComment
{
    public record AddCommentCommand(Guid TicketId, string CommentText, string UserId, bool IsAdminOrAgent) : IRequest<Result>;
}
