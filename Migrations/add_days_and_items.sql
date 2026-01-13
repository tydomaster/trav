-- Скрипт для добавления таблиц Days и Items в существующую базу данных

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
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Days_TripId_Date" ON "Days" ("TripId", "Date");
CREATE INDEX IF NOT EXISTS "IX_Items_DayId" ON "Items" ("DayId");

-- Помечаем миграцию как применённую
INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260113213944_AddDaysAndItems', '8.0.0');

