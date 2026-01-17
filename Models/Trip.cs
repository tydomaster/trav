namespace TravelPlanner.Api.Models;

public class Trip
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? HeroImageUrl { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Invite> Invites { get; set; } = new List<Invite>();
    public ICollection<Day> Days { get; set; } = new List<Day>();
    public ICollection<List> Lists { get; set; } = new List<List>();
    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    public ICollection<Note> Notes { get; set; } = new List<Note>();
    public ICollection<Place> Places { get; set; } = new List<Place>();
}

