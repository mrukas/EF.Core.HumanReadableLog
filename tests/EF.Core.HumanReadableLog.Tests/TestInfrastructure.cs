using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EF.Core.HumanReadableLog;
using EF.Core.HumanReadableLog.Attributes;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF.Core.HumanReadableLog.Tests;

public sealed class TestAuditSink : IAuditEventSink
{
    public List<string> Messages { get; } = new();
    public Task WriteAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
    {
        Messages.AddRange(messages);
        return Task.CompletedTask;
    }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<M2MUser> M2MUsers => Set<M2MUser>();
    public DbSet<M2MRole> M2MRoles => Set<M2MRole>();
    public DbSet<EF.Core.HumanReadableLog.Tests.AuditTests.TypeEntity> TypeEntities => Set<EF.Core.HumanReadableLog.Tests.AuditTests.TypeEntity>();
    public DbSet<OwnedOwner> Owners => Set<OwnedOwner>();
    public DbSet<EF.Core.HumanReadableLog.Tests.AuditTests.TypeEntity2> TypeEntity2s => Set<EF.Core.HumanReadableLog.Tests.AuditTests.TypeEntity2>();
    public DbSet<M2MLeft> Lefts => Set<M2MLeft>();
    public DbSet<M2MRight> Rights => Set<M2MRight>();
    public DbSet<O2OUser> O2OUsers => Set<O2OUser>();
    public DbSet<O2OProfile> O2OProfiles => Set<O2OProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Pets)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<M2MUser>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users);

        modelBuilder.Entity<OwnedOwner>()
            .OwnsOne(o => o.Address);

        // Composite keys setup
        modelBuilder.Entity<M2MLeft>().HasKey(l => new { l.L1, l.L2 });
        modelBuilder.Entity<M2MRight>().HasKey(r => new { r.R1, r.R2 });
        modelBuilder.Entity<M2MLeft>()
            .HasMany(l => l.Rights)
            .WithMany(r => r.Lefts)
            .UsingEntity<Dictionary<string, object>>(
                "LeftRight",
                j => j.HasOne<M2MRight>().WithMany().HasForeignKey("R1", "R2"),
                j => j.HasOne<M2MLeft>().WithMany().HasForeignKey("L1", "L2")
            );

        // One-to-one explicit
        modelBuilder.Entity<O2OUser>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<O2OProfile>(p => p.UserId);
    }
}

[AuditEntityDisplay("Note", "Notes")]
[AuditEntityTitleTemplate("{Meta.Title} - {Id}")]
public class Note
{
    public int Id { get; set; }
    [NotMapped]
    public NoteMeta Meta { get; set; } = new();
}

public class NoteMeta
{
    public string Title { get; set; } = string.Empty;
}
