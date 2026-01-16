using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Api.Data;
using TravelPlanner.Api.Middleware;
using TravelPlanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=travelplanner.db";

// Для production: создаем директорию для базы данных, если её нет
if (builder.Environment.IsProduction())
{
    var dbPath = connectionString.Replace("Data Source=", "").Split(';')[0];
    var dbDirectory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
    {
        Directory.CreateDirectory(dbDirectory);
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Services
builder.Services.AddScoped<ITelegramAuthService, TelegramAuthService>();

// External services with fallback to mocks
var openAiKey = builder.Configuration["OPENAI_API_KEY"];
if (!string.IsNullOrEmpty(openAiKey))
{
    // TODO: Add real OpenAI provider when needed
    builder.Services.AddScoped<ILlmProvider, MockLlmProvider>();
}
else
{
    builder.Services.AddScoped<ILlmProvider, MockLlmProvider>();
}

var placesApiKey = builder.Configuration["PLACES_API_KEY"]; // Google Places or Foursquare
if (!string.IsNullOrEmpty(placesApiKey))
{
    // TODO: Add real Places provider when needed
    builder.Services.AddScoped<IPlacesProvider, MockPlacesProvider>();
}
else
{
    builder.Services.AddScoped<IPlacesProvider, MockPlacesProvider>();
}

// CORS для работы с фронтендом и Telegram
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    
    // Политика для Telegram Web Apps (разрешаем все origins)
    options.AddPolicy("TelegramWebApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Логируем переменные окружения при старте (только в production для отладки)
if (app.Environment.IsProduction())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var telegramVars = Environment.GetEnvironmentVariables()
        .Cast<System.Collections.DictionaryEntry>()
        .Where(e => e.Key?.ToString()?.StartsWith("Telegram", StringComparison.OrdinalIgnoreCase) == true)
        .Select(e => $"{e.Key}={((e.Value?.ToString()?.Length ?? 0) > 0 ? "***SET***" : "EMPTY")}")
        .ToList();
    
    if (telegramVars.Any())
    {
        logger.LogInformation("Telegram environment variables at startup: {Vars}", string.Join(", ", telegramVars));
    }
    else
    {
        logger.LogWarning("No Telegram environment variables found at startup!");
    }
    
    // Также проверяем через конфигурацию
    var botTokenFromConfig = builder.Configuration["Telegram:BotToken"];
    var botSecretFromConfig = builder.Configuration["Telegram:BotSecretKey"];
    logger.LogInformation("Configuration check - Telegram:BotToken: {HasToken}, Telegram:BotSecretKey: {HasSecret}", 
        !string.IsNullOrEmpty(botTokenFromConfig), !string.IsNullOrEmpty(botSecretFromConfig));
}

// Configure the HTTP request pipeline.
// Включаем Swagger только в Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure database is created
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error creating database. Connection string: {ConnectionString}", connectionString);
    // Продолжаем выполнение, база данных будет создана при первом запросе
}

// Используем политику для Telegram Web Apps (разрешает все origins)
app.UseCors("TelegramWebApp");
app.UseTelegramAuth();
app.UseAuthorization();
app.MapControllers();

app.Run();

