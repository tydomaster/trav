-- Migration: Add HeroImageUrl column to Trips table
-- Run this script if the column doesn't exist

-- Check if column exists and add it if not
-- SQLite doesn't support IF NOT EXISTS for ALTER TABLE ADD COLUMN
-- So we check if column exists first by trying to query it

-- For SQLite, we can use this approach:
-- 1. Check if column exists (try to read from it)
-- 2. If it doesn't exist (error), add it

-- In production, this will be handled by Entity Framework migrations
-- But if migrations are not available, this SQL script can be used

-- Note: This is a simple approach. In production, use EF Core migrations:
-- dotnet ef migrations add AddHeroImageUrlToTrip
-- dotnet ef database update

-- For now, we'll add the column directly:
ALTER TABLE "Trips" ADD COLUMN "HeroImageUrl" TEXT NULL;
