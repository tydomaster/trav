namespace TravelPlanner.Api.Models;

public class Item
{
    public int Id { get; set; }
    public int DayId { get; set; }
    public TimeSpan? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? PlaceId { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Day Day { get; set; } = null!;
}

