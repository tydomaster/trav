namespace TravelPlanner.Api.Models;

public enum ListType
{
    Packing = 1,
    Todo = 2,
    Shopping = 3
}

public class List
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public ListType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int? AddedByUserId { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public User? AddedBy { get; set; }
}
