# Скрипт для применения миграции Days и Items
$dbPath = "travelplanner.db"

if (-not (Test-Path $dbPath)) {
    Write-Host "Database file not found: $dbPath"
    exit 1
}

# SQL команды
$sql = @"
-- Создание таблицы Days
CREATE TABLE IF NOT EXISTS "Days" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Days" PRIMARY KEY AUTOINCREMENT,
    "TripId" INTEGER NOT NULL,
    "Date" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Days_Trips_TripId" FOREIGN KEY ("TripId") REFERENCES "Trips" ("Id") ON DELETE CASCADE
);

-- Создание таблицы Items
CREATE TABLE IF NOT EXISTS "Items" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Items" PRIMARY KEY AUTOINCREMENT,
    "DayId" INTEGER NOT NULL,
    "StartTime" TEXT NULL,
    "DurationMinutes" INTEGER NULL,
    "Title" TEXT NOT NULL,
    "PlaceId" INTEGER NULL,
    "Notes" TEXT NULL,
    "Order" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_Items_Days_DayId" FOREIGN KEY ("DayId") REFERENCES "Days" ("Id") ON DELETE CASCADE
);

-- Создание индексов
CREATE INDEX IF NOT EXISTS "IX_Days_TripId_Date" ON "Days" ("TripId", "Date");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Days_TripId_Date_Unique" ON "Days" ("TripId", "Date");
CREATE INDEX IF NOT EXISTS "IX_Items_DayId" ON "Items" ("DayId");

-- Помечаем миграцию как применённую
INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260113213944_AddDaysAndItems', '8.0.0');
"@

# Загружаем System.Data.SQLite через .NET
Add-Type -Path "System.Data.SQLite.dll" -ErrorAction SilentlyContinue

if (-not ([System.Data.SQLite.SQLiteConnection]::new)) {
    Write-Host "SQLite library not found. Trying alternative method..."
    
    # Альтернативный способ через dotnet ef
    Write-Host "Please run the following SQL commands manually:"
    Write-Host $sql
    exit 0
}

try {
    $connectionString = "Data Source=$dbPath"
    $connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.ExecuteNonQuery()
    
    $connection.Close()
    Write-Host "Migration applied successfully!"
} catch {
    Write-Host "Error applying migration: $_"
    exit 1
}

