using FunctionTodo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FunctionTodo.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Todo>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(100);
            e.HasOne(t => t.User)
             .WithMany(u => u.Todos)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        NormalizeDueDates();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDueDates();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDueDates()
    {
        foreach (var entry in ChangeTracker.Entries<Todo>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.DueDate = DateTime.SpecifyKind(entry.Entity.DueDate, DateTimeKind.Utc);
            }
        }
    }
}
