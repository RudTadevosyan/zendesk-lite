using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Infrastructure.Configurations;

public sealed class TicketAuditLogConfiguration : IEntityTypeConfiguration<TicketAuditLog>
{
    public void Configure(EntityTypeBuilder<TicketAuditLog> builder)
    {
        builder.ToTable("ticket_audit_logs");
        builder.HasKey(al => al.Id);

        // Column Constraints
        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.ChangedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(al => al.Notes)
            .HasMaxLength(1000);

        // Relationship: Audit Log -> Ticket
        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(al => al.TicketId)
            .OnDelete(DeleteBehavior.Cascade); // If a ticket is dropped, wipe its audit history too
    }
}