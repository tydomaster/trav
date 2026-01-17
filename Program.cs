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
    // Политика для Telegram Web Apps и Vercel (разрешаем все origins)
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
    
    // Альтернативная политика с конкретными origins (если нужна более строгая настройка)
    options.AddPolicy("SpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://travel-vite-olive.vercel.app",
                "https://*.vercel.app" // Для всех поддоменов Vercel
              )
              .SetIsOriginAllowedToAllowWildcardSubdomains()
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

// Ensure database is created and migrations are applied
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Checking database...");
        
        // Проверяем, существует ли база данных
        if (!db.Database.CanConnect())
        {
            logger.LogInformation("Database does not exist. Creating...");
            db.Database.EnsureCreated();
            logger.LogInformation("Database created successfully.");
        }
        else
        {
            logger.LogInformation("Database exists. Ensuring schema is up to date...");
            // Применяем миграции, если они есть
            try
            {
                var pendingMigrations = db.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    db.Database.Migrate();
                    logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("No pending migrations. Database is up to date.");
                }
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning(migrationEx, "Could not apply migrations. Using EnsureCreated as fallback.");
                // Если миграции не работают, используем EnsureCreated
                db.Database.EnsureCreated();
            }
            
            // Убеждаемся, что таблица Flights существует (если её нет в миграциях)
            try
            {
                var connection = db.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='Flights';
                ";
                var tableExists = command.ExecuteScalar() != null;
                
                if (!tableExists)
                {
                    logger.LogInformation("Flights table does not exist. Creating...");
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS ""Flights"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Flights"" PRIMARY KEY AUTOINCREMENT,
                            ""TripId"" INTEGER NOT NULL,
                            ""Category"" INTEGER NOT NULL,
                            ""Type"" INTEGER NOT NULL,
                            ""Title"" TEXT NOT NULL,
                            ""Subtitle"" TEXT NULL,
                            ""From"" TEXT NULL,
                            ""To"" TEXT NULL,
                            ""Date"" TEXT NOT NULL,
                            ""Time"" TEXT NULL,
                            ""Status"" INTEGER NULL,
                            ""Details"" TEXT NULL,
                            ""CreatedAt"" TEXT NOT NULL,
                            ""UpdatedAt"" TEXT NOT NULL,
                            CONSTRAINT ""FK_Flights_Trips_TripId"" FOREIGN KEY (""TripId"") REFERENCES ""Trips"" (""Id"") ON DELETE CASCADE
                        );
                        CREATE INDEX IF NOT EXISTS ""IX_Flights_TripId"" ON ""Flights"" (""TripId"");
                    ";
                    command.ExecuteNonQuery();
                    logger.LogInformation("Flights table created successfully.");
                }
                else
                {
                    logger.LogInformation("Flights table already exists.");
                }
            }
            catch (Exception tableEx)
            {
                logger.LogWarning(tableEx, "Could not check/create Flights table. Will rely on EnsureCreated.");
                // Пробуем EnsureCreated как последний вариант
                db.Database.EnsureCreated();
            }
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error initializing database. Connection string: {ConnectionString}", connectionString);
    // Продолжаем выполнение, база данных будет создана при первом запросе
}

// CORS должен быть установлен ДО всех middleware, которые могут записывать ответы
app.UseCors();

// Exception handling для обработки ошибок (встроенный middleware корректно работает с CORS)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        if (exception != null)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Internal Server Error",
            message = app.Environment.IsDevelopment() && exception != null 
                ? exception.Message 
                : "An error occurred while processing your request",
            stackTrace = app.Environment.IsDevelopment() && exception != null 
                ? exception.StackTrace 
                : null
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});

app.UseTelegramAuth();
app.UseAuthorization();
app.MapControllers();

app.Run();

