namespace TravelPlanner.Api.Models;

public enum MembershipRole
{
    Owner = 1,
    Editor = 2,
    Viewer = 3
}

public class Membership
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int UserId { get; set; }
    public MembershipRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
}

