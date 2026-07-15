using ZendeskLite.Domain.Enums;

namespace ZendeskLite.Application.DTOs
{ 
    public record AdminTicketDto(
        Guid Id,
        string Title,
        string RawDescription,
        string? CleanedDescription,
        TicketStatus Status,
        TicketCategory Category,
        string? Comments,
        DateTimeOffset CreatedAt,
        // Admin/Agent specific fields
        DateTimeOffset UpdatedAt,
        DateTimeOffset LastModifiedAt,
        bool IsDeleted,
        TicketPriority Priority,
        string CustomerId,
        string? AgentId,
        List<TicketAuditLogDto> AuditLogs
    ) : BaseTicketDto(Id, Title, RawDescription, CleanedDescription, Status, Category, Comments, CreatedAt);

    public record TicketAuditLogDto(string Action, string ChangedByUserId, string? Notes, DateTimeOffset CreatedAt);
}
