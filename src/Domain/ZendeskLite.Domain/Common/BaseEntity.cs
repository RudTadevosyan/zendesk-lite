using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZendeskLite.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private init; } = Guid.NewGuid();
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastModifiedAt { get; private set; } = DateTimeOffset.UtcNow;
        public void UpdateLastModified()
        {
            LastModifiedAt = DateTimeOffset.UtcNow;
        }
        public bool IsDeleted { get; private set; } = false;

        public void SoftDelete()
        {
            IsDeleted = true;
            UpdateLastModified();
        }
    }
}
