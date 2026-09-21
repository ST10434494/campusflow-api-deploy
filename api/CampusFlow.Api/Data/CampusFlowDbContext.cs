using CampusFlow.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Data;

public sealed class CampusFlowDbContext(DbContextOptions<CampusFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<UserProfileEntity> Users => Set<UserProfileEntity>();
    public DbSet<UserSettingsEntity> UserSettings => Set<UserSettingsEntity>();
    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();
    public DbSet<TimetableEventEntity> TimetableEvents => Set<TimetableEventEntity>();
    public DbSet<AssignmentEntity> Assignments => Set<AssignmentEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfileEntity>()
            .HasIndex(user => user.FirebaseUid)
            .IsUnique();

        modelBuilder.Entity<UserProfileEntity>()
            .HasOne(user => user.Settings)
            .WithOne(settings => settings.User)
            .HasForeignKey<UserSettingsEntity>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // WithMany() with no argument because UserProfileEntity does not
        // currently expose a Modules collection.
        modelBuilder.Entity<ModuleEntity>()
            .HasOne(module => module.User)
            .WithMany()
            .HasForeignKey(module => module.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimetableEventEntity>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict module deletion when timetable events still reference it.
        // The controller handles this cleanly with a 409 response.
        modelBuilder.Entity<TimetableEventEntity>()
            .HasOne(e => e.Module)
            .WithMany(m => m.TimetableEvents)
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssignmentEntity>()
            .HasOne(assignment => assignment.User)
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Assignments must be removed before their module can be deleted.
        modelBuilder.Entity<AssignmentEntity>()
            .HasOne(assignment => assignment.Module)
            .WithMany()
            .HasForeignKey(assignment => assignment.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Every notification belongs to a user. If the user is removed,
        // their notifications should also be removed.
        modelBuilder.Entity<NotificationEntity>()
            .HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A notification can optionally reference an assignment.
        // Deleting the assignment also removes its associated reminders,
        // preventing stale assignment notifications from remaining.
        modelBuilder.Entity<NotificationEntity>()
            .HasOne(notification => notification.Assignment)
            .WithMany()
            .HasForeignKey(notification => notification.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
