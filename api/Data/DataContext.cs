using api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.ExternalId).IsUnique();
            entity.Property(u => u.ExternalId).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasIndex(t => t.DueDate);
            entity.HasIndex(t => t.ItemStatus);
            entity.HasIndex(t => t.CreatedByUserId);
            entity.HasIndex(t => t.AssignedToUserId);
            entity.HasIndex(t => new { t.CreatedByUserId, t.ItemStatus });
            entity.HasIndex(t => new { t.AssignedToUserId, t.ItemStatus });

            entity.Property(t => t.Title).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(500);

            entity.HasOne(t => t.CreatedByUser)
                  .WithMany() 
                  .HasForeignKey(t => t.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict); 

            entity.HasOne(t => t.AssignedToUser)
                  .WithMany() 
                  .HasForeignKey(t => t.AssignedToUserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
