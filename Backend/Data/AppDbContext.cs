using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Data.Entities.Common;

namespace Spot4Hire.Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<OpeningHour> OpeningHours => Set<OpeningHour>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Venue>()
            .Property(v => v.VenueType)
            .HasConversion<string>()
            .HasMaxLength(30);

        // The venue's owner. Restrict on delete so a user cannot be removed
        // while they still own venues, and to avoid a second cascade path to
        // Bookings (which SQL Server rejects).
        builder.Entity<Venue>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Exclude soft-deleted rows from every query by default.
        // Applied to all ISoftDeletable entities so it is never forgotten
        // when a new soft-deletable entity is added.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var filter = Expression.Lambda(
                Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?))),
                parameter);

            builder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken token = default)
    {
        ApplyAuditInfo();
        return await base.SaveChangesAsync(token);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = now;
                    if (entry.Entity is IAuditable auditable)
                    {
                        auditable.UpdatedAt = now;
                    }

                    break;
                }
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Modified:
                case EntityState.Added:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
