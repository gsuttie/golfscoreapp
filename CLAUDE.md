# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run the application (starts on https://localhost:7049)
dotnet run

# Apply EF Core migrations
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Watch mode for development
dotnet watch
```

## Architecture Overview

GolfScorer is a Blazor Server application (.NET 10) for tracking golf scores and statistics, using ASP.NET Core Identity for authentication.

### Data Model

- **Course**: Golf courses with per-hole par configuration stored as comma-separated string (`HolePars`). Courses can be user-owned or public.
- **Round**: A played round linked to a user and course. Stores denormalized totals (`TotalScore`, `TotalPutts`, `GIRCount`, `FairwaysHit`) for query performance.
- **HoleScore**: Individual hole data including score, putts, GIR, and fairway hit. Par 3s have nullable `FairwayHit`.

All entities are user-scoped via `UserId` foreign key to `ApplicationUser`.

### Service Layer

Services in `/Services` handle all database operations and are registered as scoped:
- **CourseService**: CRUD for courses, respects user ownership and public visibility
- **RoundService**: CRUD for rounds with hole scores, calculates denormalized totals on save
- **StatisticsService**: Aggregates round data into `GolfStatistics` including handicap index calculation (simplified USGA formula using slope/course rating)

### Blazor Components

- `/Components/Pages/Courses/` - Course management (list, edit)
- `/Components/Pages/Rounds/` - Round entry and viewing (list, entry, details)
- `/Components/Pages/Statistics/` - Dashboard with aggregated stats
- `/Components/Account/` - Identity scaffolded pages (login, register, account management)

### Database

Uses SQL Server LocalDB by default (`appsettings.json`). SQLite package is included for alternative configuration. The `Data/app.db` SQLite file exists for local development.

EF Core migrations are in `/Migrations`. The context uses Fluent API configuration in `ApplicationDbContext.OnModelCreating` for relationships and indexes.
