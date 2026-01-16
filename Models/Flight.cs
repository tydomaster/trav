namespace TravelPlanner.Api.Models;

public enum BookingCategory
{
    Transport = 1,
    Accommodation = 2,
    Places = 3
}

public enum BookingType
{
    Flight = 1,
    Hotel = 2,
    Transfer = 3,
    Restaurant = 4,
    Bus = 5,
    Train = 6,
    Hostel = 7
}

public enum BookingStatus
{
    OnTime = 1,
    Delayed = 2,
    Cancelled = 3,
    Confirmed = 4
}

public class Flight
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public BookingCategory Category { get; set; }
    public BookingType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan? Time { get; set; }
    public BookingStatus? Status { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
}
