namespace TravelPlanner.Api.DTOs;

public class ListDto
{
    public int Id { get; set; }
    public int TripId { get; set; }
    public int Type { get; set; } // 1 = Packing, 2 = Todo, 3 = Shopping
    public string Text { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public int? AddedByUserId { get; set; }
    public string? AddedByName { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateListItemDto
{
    public int Type { get; set; } // 1 = Packing, 2 = Todo, 3 = Shopping
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateListItemDto
{
    public string? Text { get; set; }
    public bool? Completed { get; set; }
    public int? Order { get; set; }
}

public class ReorderListItemsDto
{
    public List<int> ItemIds { get; set; } = new();
}
