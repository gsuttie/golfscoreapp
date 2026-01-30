# GolfScorer

A Blazor Server application for tracking golf rounds, managing courses, and analyzing performance statistics.

## Features

### Course Management
- Create and manage custom golf courses with per-hole par configuration
- Configure slope rating and course rating for handicap calculations
- Support for public and private (user-owned) courses

### Round Recording
- Record complete rounds with hole-by-hole scoring
- Track for each hole:
  - Score and putts
  - Green in Regulation (GIR)
  - Fairway hit (Par 4s and 5s only)
- Add weather conditions and notes

### Statistics Dashboard
- **Scoring**: Average score, best/worst rounds, par-specific averages
- **Putting**: Average putts, 1-putt/2-putt/3-putt percentages
- **Performance**: GIR percentage, fairway hit percentage
- **Score Distribution**: Eagles, birdies, pars, bogeys breakdown
- **Handicap Index**: Calculated using simplified USGA formula
- **Per-hole Analysis**: Average score and GIR for each hole
- Year-based filtering

## Tech Stack

- .NET 10 / ASP.NET Core
- Blazor Server (interactive server-side rendering)
- Entity Framework Core 10
- ASP.NET Core Identity
- SQL Server LocalDB (SQLite alternative available)
- Bootstrap 5

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (included with Visual Studio) or SQLite

### Setup

1. Clone the repository
   ```bash
   git clone <repository-url>
   cd golfapp
   ```

2. Apply database migrations
   ```bash
   dotnet ef database update
   ```

3. Run the application
   ```bash
   dotnet run
   ```

4. Open https://localhost:7049 in your browser

### Development

```bash
# Watch mode with hot reload
dotnet watch

# Add a new migration
dotnet ef migrations add <MigrationName>
```

## Project Structure

```
├── Components/
│   ├── Account/          # Identity pages (login, register)
│   ├── Layout/           # App layout components
│   └── Pages/
│       ├── Courses/      # Course management
│       ├── Rounds/       # Round entry and viewing
│       └── Statistics/   # Stats dashboard
├── Data/
│   └── ApplicationDbContext.cs
├── Migrations/           # EF Core migrations
├── Models/
│   ├── Course.cs         # Golf course entity
│   ├── Round.cs          # Round entity with denormalized totals
│   ├── HoleScore.cs      # Individual hole data
│   └── GolfStatistics.cs # Statistics DTO
├── Services/
│   ├── CourseService.cs      # Course CRUD
│   ├── RoundService.cs       # Round CRUD with calculations
│   └── StatisticsService.cs  # Stats aggregation
└── Program.cs
```

## Data Model

- **Course**: Golf courses with 18-hole par configuration, slope/course rating
- **Round**: Played rounds with denormalized totals for query performance
- **HoleScore**: Per-hole data including score, putts, GIR, fairway hit

All data is user-scoped via ASP.NET Core Identity.

## License

MIT
