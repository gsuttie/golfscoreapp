using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GolfScorer.Models;

namespace GolfScorer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<HoleScore> HoleScores => Set<HoleScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Course configuration
        builder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.UserId);
            entity.Property(c => c.SlopeRating).HasPrecision(5, 1);
            entity.Property(c => c.CourseRating).HasPrecision(4, 1);
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Round configuration
        builder.Entity<Round>(entity =>
        {
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.DatePlayed);
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Course)
                .WithMany(c => c.Rounds)
                .HasForeignKey(r => r.CourseId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // HoleScore configuration
        builder.Entity<HoleScore>(entity =>
        {
            entity.HasIndex(h => h.RoundId);
            entity.HasOne(h => h.Round)
                .WithMany(r => r.HoleScores)
                .HasForeignKey(h => h.RoundId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
