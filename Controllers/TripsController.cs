using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.DTOs;
using TravelPlanner.Api.Extensions;
using TravelPlanner.Api.Models;
using System.Text.Json;
using System.Text;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public TripsController(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

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

    // PUT: api/trips/{id} - Обновить поездку
    [HttpPut("{id}")]
    public async Task<ActionResult<TripDto>> UpdateTrip(int id, UpdateTripDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var userIdValue = userId.Value;

        var trip = await _context.Trips
            .Include(t => t.Owner)
            .Include(t => t.Memberships)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null)
            return NotFound();

        // Проверяем доступ (только owner и editor могут обновлять)
        var membership = trip.Memberships.FirstOrDefault(m => m.UserId == userIdValue);
        if (membership == null || 
            (membership.Role != MembershipRole.Owner && membership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can update trips" });

        // Обновляем поля
        if (dto.Title != null)
            trip.Title = dto.Title;
        if (dto.StartDate.HasValue)
            trip.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue)
            trip.EndDate = dto.EndDate.Value;
        
        trip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Загружаем обновленные данные
        await _context.Entry(trip)
            .Reference(t => t.Owner)
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
            return StatusCode(403, new { error = "Access denied", message = "Only trip owner can change member roles" });

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

    // DELETE: api/trips/{tripId}/members/{userId} - Удалить участника из поездки
    [HttpDelete("{tripId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int tripId, int userId)
    {
        var currentUserId = User.GetUserId();
        if (currentUserId == null)
            return Unauthorized();

        var currentUserIdValue = currentUserId.Value;

        var trip = await _context.Trips
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
            return NotFound();

        // Проверяем, что текущий пользователь - owner или editor
        var currentMembership = trip.Memberships.FirstOrDefault(m => m.UserId == currentUserIdValue);
        if (currentMembership == null || 
            (currentMembership.Role != MembershipRole.Owner && currentMembership.Role != MembershipRole.Editor))
            return StatusCode(403, new { error = "Access denied", message = "Only owner and editor can remove members" });

        // Находим membership для удаления
        var targetMembership = trip.Memberships.FirstOrDefault(m => m.UserId == userId);
        if (targetMembership == null)
            return NotFound();

        // Нельзя удалить owner
        if (targetMembership.Role == MembershipRole.Owner)
            return BadRequest(new { error = "Cannot remove trip owner" });

        // Нельзя удалить самого себя (только owner может удалять других)
        if (userId == currentUserIdValue && currentMembership.Role != MembershipRole.Owner)
            return BadRequest(new { error = "Cannot remove yourself. Only owner can remove members" });

        _context.Memberships.Remove(targetMembership);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/trips/{tripId}/export-pdf - Экспортировать поездку в PDF и отправить в Telegram
    [HttpPost("{tripId}/export-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExportTripToPdf(int tripId, IFormFile pdfData)
    {
        var userId = User.GetUserId();
        var telegramId = User.GetTelegramId();
        
        if (userId == null || telegramId == null)
            return Unauthorized();

        var userIdValue = userId.Value;
        var telegramIdValue = telegramId.Value;

        // Проверяем доступ к поездке
        var trip = await _context.Trips
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
            return NotFound();

        var membership = await _context.Memberships
            .FirstOrDefaultAsync(m => m.TripId == tripId && m.UserId == userIdValue);

        if (membership == null)
            return StatusCode(403, new { error = "Access denied", message = "You are not a member of this trip" });

        if (pdfData == null || pdfData.Length == 0)
        {
            return BadRequest(new { error = "PDF file is required" });
        }

        // Отправляем PDF в Telegram через Bot API
        try
        {
            var botToken = _configuration["Telegram:BotToken"] 
                        ?? _configuration["Telegram:BotSecretKey"] 
                        ?? "";

            if (string.IsNullOrEmpty(botToken))
            {
                return StatusCode(500, new { error = "Bot token not configured" });
            }

            var httpClient = _httpClientFactory.CreateClient();
            var botApiUrl = $"https://api.telegram.org/bot{botToken}/sendDocument";

            // Создаем multipart form data для Telegram Bot API
            using var formData = new MultipartFormDataContent();
            
            // Добавляем chat_id
            formData.Add(new StringContent(telegramIdValue.ToString()), "chat_id");
            
            // Читаем файл в массив байтов
            byte[] fileBytes;
            using (var fileStream = pdfData.OpenReadStream())
            {
                fileBytes = new byte[pdfData.Length];
                await fileStream.ReadAsync(fileBytes, 0, (int)pdfData.Length);
            }
            
            // Формируем безопасное имя файла (только латиница, цифры, подчеркивания и дефисы)
            var safeTitle = System.Text.RegularExpressions.Regex.Replace(trip.Title, @"[^a-zA-Z0-9\s_-]", "");
            safeTitle = safeTitle.Replace(" ", "_");
            if (string.IsNullOrWhiteSpace(safeTitle))
                safeTitle = "trip";
            var fileName = $"{safeTitle}_{DateTime.UtcNow:yyyy-MM-dd}.pdf";
            
            // Создаем ByteArrayContent для файла (Telegram Bot API требует правильный формат)
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            
            // Добавляем файл с правильным именем для Telegram Bot API
            // Формат: Add(content, "fieldName", "fileName")
            // Имя параметра должно быть "document" для sendDocument
            formData.Add(fileContent, "document", fileName);

            // Отправляем запрос
            var response = await httpClient.PostAsync(botApiUrl, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<TripsController>>();
                logger.LogError("Failed to send PDF to Telegram: {StatusCode} {Content}", response.StatusCode, responseContent);
                return StatusCode(500, new { error = "Failed to send PDF to Telegram", details = responseContent });
            }

            return Ok(new { message = "PDF sent successfully to Telegram" });
        }
        catch (Exception ex)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<TripsController>>();
            logger.LogError(ex, "Error sending PDF to Telegram");
            return StatusCode(500, new { error = "Error sending PDF to Telegram", message = ex.Message });
        }
    }
}
