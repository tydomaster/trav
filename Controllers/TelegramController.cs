using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Api.Services;

namespace TravelPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly ITelegramAuthService _authService;
    private readonly IConfiguration _configuration;

    public TelegramController(ITelegramAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    // POST: api/telegram/validate - Валидация initData
    [HttpPost("validate")]
    public IActionResult ValidateInitData([FromBody] ValidateInitDataDto dto)
    {
        if (string.IsNullOrEmpty(dto.InitData))
            return BadRequest(new { error = "initData is required" });

        var botToken = _configuration["Telegram:BotToken"]
                      ?? _configuration["Telegram:BotSecretKey"]
                      ?? "";
        var isDevelopment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";

        // В dev режиме пропускаем валидацию
        var isValid = isDevelopment || _authService.ValidateInitData(dto.InitData, botToken);

        if (!isValid)
            return Unauthorized(new { error = "Invalid initData" });

        var userData = _authService.ParseInitData(dto.InitData);

        return Ok(new
        {
            valid = true,
            user = userData
        });
    }
}

public class ValidateInitDataDto
{
    public string InitData { get; set; } = string.Empty;
}

