using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    public DbSet<Invite> Invites { get; set; }
    public DbSet<Day> Days { get; set; }
    public DbSet<Item> Items { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramId).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // Trip configuration
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Membership configuration
        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TripId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Memberships)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Memberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invite configuration
        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(64);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Invites)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Day configuration
        modelBuilder.Entity<Day>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TripId, e.Date }).IsUnique();
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Days)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Item configuration
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasOne(e => e.Day)
                .WithMany(d => d.Items)
                .HasForeignKey(e => e.DayId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

