namespace TravelPlanner.Api.Models;

public class Day
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

