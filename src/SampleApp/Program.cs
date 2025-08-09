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

        var user = new User();
        user.Pets.Add(new Pet { Name = "Schnuffi" });

        db.Users.Add(user);
        await db.SaveChangesAsync();

        user.Pets.Add(new Pet { Name = "Bello" });
        await db.SaveChangesAsync();

        var firstPet = await db.Pets.FirstAsync();
        db.Remove(firstPet);
        await db.SaveChangesAsync();
    }
}
