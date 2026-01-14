using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TripsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/trips - Получить мои поездки
    [HttpGet]
    public async Task<ActionResult<List<TripDto>>> GetMyTrips()
    {
        var userId = User.GetUserId();
        var telegramId = User.GetTelegramId();
        
        // Логируем для отладки
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<TripsController>>();
        logger.LogInformation("GetMyTrips called - UserId: {UserId}, TelegramId: {TelegramId}", userId, telegramId);
        
        if (userId == null)
        {
            logger.LogWarning("GetMyTrips - UserId is null, user not authenticated");
            return Unauthorized(new { 
                error = "Unauthorized", 
                message = "User not authenticated. Please ensure you are accessing from Telegram Mini App." 
            });
        }

        var userIdValue = userId.Value;
        logger.LogInformation("GetMyTrips - Filtering trips for UserId: {UserId}", userIdValue);

        var trips = await _context.Trips
            .Include(t => t.Owner)
            .Include(t => t.Memberships)
                .ThenInclude(m => m.User)
            .Where(t => t.Memberships.Any(m => m.UserId == userIdValue))
            .Select(t => new TripDto
            {
                Id = t.Id,
                Title = t.Title,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                OwnerId = t.OwnerId,
                OwnerName = t.Owner.Name,
                Role = (MembershipRoleDto)t.Memberships.First(m => m.UserId == userIdValue).Role,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Members = t.Memberships.Select(m => new MemberDto
                {
                    UserId = m.UserId,
                    Name = m.User.Name,
                    Avatar = m.User.Avatar,
                    Role = (MembershipRoleDto)m.Role
                }).ToList()
            })
            .ToListAsync();

        logger.LogInformation("GetMyTrips - Found {Count} trips for UserId: {UserId}", trips.Count, userIdValue);
        if (trips.Any())
        {
            logger.LogInformation("GetMyTrips - Trip IDs: {TripIds}", string.Join(", ", trips.Select(t => t.Id)));
        }

        return Ok(trips);
    }

    // GET: api/trips/5 - Получить поездку
    [HttpGet("{id}")]
    public async Task<ActionResult<TripDto>> GetTrip(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var trip = await _context.Trips
            .Include(t => t.Owner)
            .Include(t => t.Memberships)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null)
            return NotFound();

        var userIdValue = userId.Value;
        var membership = trip.Memberships.FirstOrDefault(m => m.UserId == userIdValue);
        if (membership == null)
            return Forbid();

        var tripDto = new TripDto
        {
            Id = trip.Id,
            Title = trip.Title,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            OwnerId = trip.OwnerId,
            OwnerName = trip.Owner.Name,
            Role = (MembershipRoleDto)membership.Role,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            Members = trip.Memberships.Select(m => new MemberDto
            {
                UserId = m.UserId,
                Name = m.User.Name,
                Avatar = m.User.Avatar,
                Role = (MembershipRoleDto)m.Role
            }).ToList()
        };

        return Ok(tripDto);
    }

    // POST: api/trips - Создать поездку
    [HttpPost]
    public async Task<ActionResult<TripDto>> CreateTrip(CreateTripDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        var user = await _context.Users.FindAsync(userIdValue);
        if (user == null)
            return Unauthorized();

        var trip = new Trip
        {
            Title = dto.Title,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OwnerId = userIdValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        // Создаем membership для владельца
        var membership = new Membership
        {
            TripId = trip.Id,
            UserId = userIdValue,
            Role = MembershipRole.Owner,
            CreatedAt = DateTime.UtcNow
        };

        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync();

        // Загружаем полные данные для ответа
        await _context.Entry(trip)
            .Reference(t => t.Owner)
            .LoadAsync();
        await _context.Entry(trip)
            .Collection(t => t.Memberships)
            .LoadAsync();
        await _context.Entry(trip)
            .Collection(t => t.Memberships)
            .Query()
            .Include(m => m.User)
            .LoadAsync();

        var tripDto = new TripDto
        {
            Id = trip.Id,
            Title = trip.Title,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            OwnerId = trip.OwnerId,
            OwnerName = trip.Owner.Name,
            Role = MembershipRoleDto.Owner,
            CreatedAt = trip.CreatedAt,
            UpdatedAt = trip.UpdatedAt,
            Members = trip.Memberships.Select(m => new MemberDto
            {
                UserId = m.UserId,
                Name = m.User.Name,
                Avatar = m.User.Avatar,
                Role = (MembershipRoleDto)m.Role
            }).ToList()
        };

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, tripDto);
    }

    // PUT: api/trips/5/members/role - Изменить роль участника (только owner)
    [HttpPut("{tripId}/members/role")]
    public async Task<IActionResult> UpdateMemberRole(int tripId, UpdateMembershipRoleDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var trip = await _context.Trips
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
            return NotFound();

        // Проверяем, что текущий пользователь - owner
        var userIdValue = userId.Value;
        var currentMembership = trip.Memberships.FirstOrDefault(m => m.UserId == userIdValue);
        if (currentMembership == null || currentMembership.Role != MembershipRole.Owner)
            return Forbid();

        // Находим membership для изменения
        var targetMembership = trip.Memberships.FirstOrDefault(m => m.UserId == dto.UserId);
        if (targetMembership == null)
            return NotFound();

        // Нельзя изменить роль owner
        if (targetMembership.Role == MembershipRole.Owner)
            return BadRequest(new { error = "Cannot change owner role" });

        targetMembership.Role = dto.Role;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
