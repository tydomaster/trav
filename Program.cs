using System.IO;
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

// CORS для работы с фронтендом и Telegram
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
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

