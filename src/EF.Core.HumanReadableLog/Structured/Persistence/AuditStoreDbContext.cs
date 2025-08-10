using Microsoft.EntityFrameworkCore;

namespace EF.Core.HumanReadableLog.Structured.Persistence;

/// <summary>
/// EF Core DbContext used to persist structured audit logs.
/// </summary>
public sealed class AuditStoreDbContext(DbContextOptions<AuditStoreDbContext> options) : DbContext(options)
{
    /// <summary>Audit events.</summary>
    internal DbSet<AuditEventRow> Events => Set<AuditEventRow>();
    /// <summary>Audit entries (per entity and root) belonging to an event.</summary>
    internal DbSet<AuditEntryRow> Entries => Set<AuditEntryRow>();
    /// <summary>Changes within an audit entry.</summary>
    internal DbSet<AuditChangeRow> Changes => Set<AuditChangeRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use a dedicated default schema for all audit tables where supported (ignored by providers like SQLite)
        modelBuilder.HasDefaultSchema("audit");

        modelBuilder.Entity<AuditEventRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.TimestampUtc).IsRequired();
        });
        modelBuilder.Entity<AuditEntryRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.RootType, e.RootId, e.TimestampUtc });
            b.HasOne(e => e.Event)
                .WithMany(e => e.Entries)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AuditChangeRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasOne(e => e.Entry)
                .WithMany(e => e.Changes)
                .HasForeignKey(e => e.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
