namespace TravelPlanner.Api.Models;

public class Invite
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; } = false;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
}

