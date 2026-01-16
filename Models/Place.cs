namespace TravelPlanner.Api.Models;

public class Place
{
    public int Id { get; set; }
    public int? TripId { get; set; } // Связь с поездкой (опционально, для мест из поиска может быть null)
    public int? AddedByUserId { get; set; } // Кто добавил место в избранное
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip? Trip { get; set; }
    public User? AddedBy { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

