using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InvitesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/invites - Создать приглашение
    [HttpPost]
    public async Task<ActionResult<object>> CreateInvite([FromBody] CreateInviteDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var trip = await _context.Trips
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == dto.TripId);

        if (trip == null)
            return NotFound();

        // Проверяем, что пользователь - owner или editor
        var userIdValue = userId.Value;
        var membership = trip.Memberships.FirstOrDefault(m => m.UserId == userIdValue);
        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can create invites" });

        // Генерируем токен
        var token = GenerateToken();

        var invite = new Invite
        {
            TripId = dto.TripId,
            Token = token,
            ExpiresAt = dto.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        _context.Invites.Add(invite);
        await _context.SaveChangesAsync();

        return Ok(new { token, expiresAt = invite.ExpiresAt });
    }

    // POST: api/invites/join - Присоединиться к поездке по токену
    [HttpPost("join")]
    public async Task<ActionResult<TripDto>> JoinTrip(JoinTripDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var invite = await _context.Invites
            .Include(i => i.Trip)
            .FirstOrDefaultAsync(i => i.Token == dto.Token);

        if (invite == null)
            return NotFound(new { error = "Invalid or expired invite" });

        if (invite.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "Invite has expired" });

        var userIdValue = userId.Value;

        // Проверяем, не является ли пользователь уже участником
        var existingMembership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == invite.TripId && m.UserId == userIdValue);

        if (existingMembership != null)
            return BadRequest(new { error = "User is already a member" });

        // Создаем membership
        var membership = new Membership
        {
            TripId = invite.TripId,
            UserId = userIdValue,
            Role = MembershipRole.Editor, // По умолчанию editor (может редактировать)
            CreatedAt = DateTime.UtcNow
        };

        _context.Memberships.Add(membership);

        // НЕ помечаем invite как использованный - разрешаем повторное использование
        // invite.IsUsed = true;

        await _context.SaveChangesAsync();

        // Загружаем полные данные поездки
        var trip = await _context.Trips
            .Include(t => t.Owner)
            .Include(t => t.Memberships)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == invite.TripId);

        if (trip == null)
            return NotFound();

        var tripDto = new TripDto
        {
            Id = trip.Id,
            Title = trip.Title,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            OwnerId = trip.OwnerId,
            OwnerName = trip.Owner.Name,
            Role = MembershipRoleDto.Editor,
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

    private string GenerateToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 32);
    }
}

public class CreateInviteDto
{
    public int TripId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

