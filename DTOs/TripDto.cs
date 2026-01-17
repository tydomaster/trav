namespace TravelPlanner.Api.DTOs;

public class TripDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? HeroImageUrl { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public MembershipRoleDto Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MemberDto> Members { get; set; } = new();
}

public class MemberDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public MembershipRoleDto Role { get; set; }
}

public enum MembershipRoleDto
{
    Owner = 1,
    Editor = 2,
    Viewer = 3
}

