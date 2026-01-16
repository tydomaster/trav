namespace TravelPlanner.Api.DTOs;

public class FlightDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int Category { get; set; } // 1 = Transport, 2 = Accommodation, 3 = Places
    public int Type { get; set; } // 1 = Flight, 2 = Hotel, etc.
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Time { get; set; } // Format: "HH:mm"
    public int? Status { get; set; } // 1 = OnTime, 2 = Delayed, 3 = Cancelled, 4 = Confirmed
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateFlightDto
{
    public int Category { get; set; }
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Time { get; set; }
    public int? Status { get; set; }
    public string? Details { get; set; }
}

public class UpdateFlightDto
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public DateTime? Date { get; set; }
    public string? Time { get; set; }
    public int? Status { get; set; }
    public string? Details { get; set; }
}
