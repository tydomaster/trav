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
    public DbSet<Place> Places { get; set; }
    public DbSet<List> Lists { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<Note> Notes { get; set; }

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
            entity.HasOne(e => e.Place)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PlaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Place configuration
        modelBuilder.Entity<Place>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Places)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AddedBy)
                .WithMany()
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // List configuration
        modelBuilder.Entity<List>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Lists)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AddedBy)
                .WithMany()
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Flight configuration
        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subtitle).HasMaxLength(200);
            entity.Property(e => e.From).HasMaxLength(100);
            entity.Property(e => e.To).HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(500);
            
            // Конвертер для TimeSpan в SQLite (хранится как строка с ticks)
            entity.Property(e => e.Time)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks.ToString() : null,
                    v => ParseTimeSpan(v));
            
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Flights)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Note configuration
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).HasMaxLength(5000);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Notes)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // Вспомогательный метод для парсинга TimeSpan (не может быть в expression tree)
    private static TimeSpan? ParseTimeSpan(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (long.TryParse(value, out var ticks))
            return TimeSpan.FromTicks(ticks);

        return null;
    }
}

