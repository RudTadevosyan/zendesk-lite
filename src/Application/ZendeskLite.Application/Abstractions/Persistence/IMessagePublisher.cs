using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Application.Abstractions.Persistence
{
    public interface IMessagePublisher
    {
        Task PublishAsync<TMessage>(TMessage message, string routingKey, CancellationToken ct = default) where TMessage : class;
        Task PublishRawAsync(Guid messageId, string payload, string routingKey, CancellationToken ct = default);
    }
}
