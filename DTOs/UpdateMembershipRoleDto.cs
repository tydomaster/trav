using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.DTOs;

public class UpdateMembershipRoleDto
{
    public int UserId { get; set; }
    public MembershipRole Role { get; set; }
}

