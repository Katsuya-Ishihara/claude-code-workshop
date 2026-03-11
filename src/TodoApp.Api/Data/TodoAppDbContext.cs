using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data.Entities;

namespace TodoApp.Api.Data;

public class TodoAppDbContext(DbContextOptions<TodoAppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.DisplayName).HasMaxLength(100);
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.AssignedToUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.DeletedAt);
            entity.HasIndex(e => e.CategoryId);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedTodos)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedTo)
                .WithMany(u => u.AssignedTodos)
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is TodoItem todo)
            {
                todo.UpdatedAt = now;
                if (entry.State == EntityState.Added)
                    todo.CreatedAt = now;
            }
            else if (entry.Entity is User user)
            {
                user.UpdatedAt = now;
                if (entry.State == EntityState.Added)
                    user.CreatedAt = now;
            }
        }
    }
}
