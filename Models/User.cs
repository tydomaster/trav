namespace TravelPlanner.Api.Models;

public class User
{
    public int Id { get; set; }
    public long TelegramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

