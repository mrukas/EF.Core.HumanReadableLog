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
                s.AddDbContext<AppDbContext>((sp, o) => o
                    .UseSqlite("Data Source=sample.db")
                    .UseAuditLogging(sp));
            })
            .Build();

        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

    Console.WriteLine("=== English (default templates; model labels are German for demo) ===");
        var user = new User();
        user.Pets.Add(new Pet { Name = "Schnuffi" });

        db.Users.Add(user);
        await db.SaveChangesAsync();

        user.Pets.Add(new Pet { Name = "Bello" });
        await db.SaveChangesAsync();

        var firstPet = await db.Pets.FirstAsync();
        db.Remove(firstPet);
        await db.SaveChangesAsync();

    // Demonstrate switching to German localizer in a fresh scope
    Console.WriteLine("=== German (configured via options; model labels: Haustier/Haustiere) ===");
    await using var scope2 = host.Services.CreateAsyncScope();
    var services = scope2.ServiceProvider;
    var options = services.GetRequiredService<EF.Core.HumanReadableLog.AuditOptions>();
    options.Localizer = new EF.Core.HumanReadableLog.Localization.GermanAuditLocalizer();

    var db2 = services.GetRequiredService<AppDbContext>();
    await db2.Database.EnsureDeletedAsync();
    await db2.Database.EnsureCreatedAsync();

    var user2 = new User();
    user2.Pets.Add(new Pet { Name = "Schnuffi" });
    db2.Users.Add(user2);
    await db2.SaveChangesAsync();

    user2.Pets.Add(new Pet { Name = "Bello" });
    await db2.SaveChangesAsync();

    var firstPet2 = await db2.Pets.FirstAsync();
    db2.Remove(firstPet2);
    await db2.SaveChangesAsync();
    }
}
