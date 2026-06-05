using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZendeskLite.Domain.Entities;

namespace ZendeskLite.Infrastructure.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(t => t.Id);

        // Column Constraints
        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.RawDescription)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(t => t.CleanedDescription)
            .HasMaxLength(4000);

        // Convert Enums to explicit text strings inside PostgreSQL
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(t => t.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relationship: Ticket -> Customer (AppUser)
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Ticket -> Agent (AppUser)
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}