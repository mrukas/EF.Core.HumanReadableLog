using EF.Core.HumanReadableLog.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
                    s.AddEfCoreAuditLogging();
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
        await auditDb.Database.EnsureCreatedAsync();
        // In real apps with migrations, prefer:
        // await db.Database.MigrateAsync();
        // await auditDb.Database.MigrateAsync();

        Console.WriteLine("=== English (default templates; model labels are German for demo) ===");
        var user = new User();
        user.Pets.Add(new Pet { Name = "Schnuffi", FavoriteFoods = { new Food { Name = "Knochen", Calories = 100 } } });

        db.Users.Add(user);
        await db.SaveChangesAsync();

        user.Pets.Add(new Pet { Name = "Bello" });
        await db.SaveChangesAsync();

        var firstPet = await db.Pets.FirstAsync();
        firstPet.FavoriteFoods.Add(new Food { Name = "Fisch", Calories = 200 });
        firstPet.FavoriteFoods.Remove(firstPet.FavoriteFoods.First());
        await db.SaveChangesAsync();

        // Demonstrate switching to German localizer in a fresh scope
        Console.WriteLine("=== German (configured via options; model labels: Haustier/Haustiere) ===");
        await using var scope2 = host.Services.CreateAsyncScope();
        var services = scope2.ServiceProvider;
        var options = services.GetRequiredService<EF.Core.HumanReadableLog.AuditOptions>();
        options.Localizer = new EF.Core.HumanReadableLog.Localization.GermanAuditLocalizer();

        var db2 = services.GetRequiredService<AppDbContext>();
        var auditDb2 = services.GetRequiredService<EF.Core.HumanReadableLog.Structured.Persistence.AuditStoreDbContext>();
        await db2.Database.EnsureDeletedAsync();
        await db2.Database.EnsureCreatedAsync();
        await auditDb2.Database.EnsureCreatedAsync();

        var user2 = new User();
        user2.Pets.Add(new Pet { Name = "Schnuffi" });
        db2.Users.Add(user2);
        await db2.SaveChangesAsync();
        var user2Id = user2.Id.ToString();

        user2.Pets.Add(new Pet { Name = "Bello" });
        await db2.SaveChangesAsync();

        var firstPet2 = await db2.Pets.FirstAsync();
        db2.Remove(firstPet2);
        await db2.SaveChangesAsync();
        // Fetch and print history for user2 as demo
        var history = services.GetRequiredService<EF.Core.HumanReadableLog.Structured.Persistence.IAuditHistoryReader>();
        await foreach (var evt in history.GetByRootAsync("User", user2Id))
        {
            foreach (var entry in evt.Entries)
            {
                foreach (var change in entry.Changes)
                {
                    Console.WriteLine($"[AUDIT] {evt.TimestampUtc:o} - {change.Message}");
                }
            }
        }

        // Filtered history: last 10 minutes, first 1 item (paging)
        Console.WriteLine("=== Filtered history (last 10 minutes, first 1) ===");
        var from = DateTime.UtcNow.AddMinutes(-10);
        await foreach (var evt in history.GetByRootAsync("User", user2Id, fromUtc: from, toUtc: null, skip: 0, take: 1))
        {
            foreach (var entry in evt.Entries)
            {
                foreach (var change in entry.Changes)
                {
                    Console.WriteLine($"[AUDIT:FILTERED] {evt.TimestampUtc:o} - {change.Message}");
                }
            }
        }
    }
}
