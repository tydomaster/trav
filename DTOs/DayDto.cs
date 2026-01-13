namespace TravelPlanner.Api.DTOs;

public class DayDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public DateTime Date { get; set; }
    public List<ItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ItemDto
{
    public int Id { get; set; }
    public int DayId { get; set; }
    public string? StartTime { get; set; } // Format: "HH:mm"
    public int? DurationMinutes { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? PlaceId { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDayDto
{
    public DateTime Date { get; set; }
}

public class CreateItemDto
{
    public string? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? PlaceId { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
}

public class UpdateItemDto
{
    public string? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? PlaceId { get; set; }
    public string? Notes { get; set; }
    public int Order { get; set; }
}

public class ReorderItemsDto
{
    public List<int> ItemIds { get; set; } = new(); // Порядок item IDs
}

