using EF.Core.HumanReadableLog.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EF.Core.HumanReadableLog.Localization;

namespace SampleApp;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Pet> Pets => Set<Pet>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Pets)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class Program
{
    public static async Task Main()
    {
        var host = Host.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    // Default: English localizer
                    s.AddEfCoreAuditLogging(opts => opts.Localizer = new GermanAuditLocalizer());
                    // Add a SQLite-based audit store next to sample.db for demo
                    s.AddEfCoreAuditStore(o => o.UseSqlite("Data Source=audit.db",
                        opts => { opts.MigrationsHistoryTable("__AuditMigrationHistory"); }));
                    s.AddDbContext<AppDbContext>((sp, o) => o
                        .UseSqlite("Data Source=sample.db")
                        .UseAuditLogging(sp));
                })
                .Build();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditDb = scope.ServiceProvider.GetRequiredService<EF.Core.HumanReadableLog.Structured.Persistence.AuditStoreDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await auditDb.Database.EnsureDeletedAsync();
        await auditDb.Database.EnsureCreatedAsync();

        var user = new User();
        user.Pets.Add(new Pet { Name = "Schnuffi", FavoriteFoods = { new Food { Name = "Knochen", Calories = 100 } } });

        db.Users.Add(user);
        await db.SaveChangesAsync();

        await using var scope2 = host.Services.CreateAsyncScope();
        var services = scope2.ServiceProvider;
        var options = services.GetRequiredService<EF.Core.HumanReadableLog.AuditOptions>();
        options.Localizer = new EF.Core.HumanReadableLog.Localization.GermanAuditLocalizer();

        var db2 = services.GetRequiredService<AppDbContext>();
        var auditDb2 = services.GetRequiredService<EF.Core.HumanReadableLog.Structured.Persistence.AuditStoreDbContext>();

        var firstPet2 = await db2.Pets.FirstAsync();

        db2.Remove(firstPet2);
        await db2.SaveChangesAsync();
        // Fetch and print history for user2 as demo
        var history = services.GetRequiredService<EF.Core.HumanReadableLog.Structured.Persistence.IAuditHistoryReader>();
        await foreach (var evt in history.GetByRootAsync("User", user.Id.ToString()))
        {
            foreach (var entry in evt.Entries)
            {
                foreach (var change in entry.Changes)
                {
                    Console.WriteLine($"[AUDIT] {evt.TimestampUtc:o} - {change.Message}");
                }
            }
        }
    }
}
