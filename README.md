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
- **Hole Difficulty Ranking**: Holes ranked hardest to easiest by average score vs par
- Year-based filtering

## Tech Stack

- .NET 10 / ASP.NET Core
- Blazor Server (interactive server-side rendering)
- Entity Framework Core 10
- ASP.NET Core Identity
- SQL Server LocalDB (SQLite alternative available)
- Bootstrap 5
- Google Fonts (Playfair Display, Outfit, DM Mono)

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

## UI Design

The app uses an "Augusta Dark" design theme — a luxury golf club aesthetic built on top of Bootstrap 5.

### Design tokens (CSS custom properties in `wwwroot/app.css`)

| Token | Value | Usage |
|---|---|---|
| `--gc-bg` | `#091510` | Page background |
| `--gc-surface` | `#0e1f15` | Card backgrounds |
| `--gc-gold` | `#c9a640` | Primary accent, buttons, icons |
| `--gc-cream` | `#f0e8d0` | Primary text |
| `--gc-birdie` | `#5cd688` | Under-par scores |
| `--gc-bogey` | `#e07060` | Over-par scores |

### Typography
- **Headings (h1–h4)**: Playfair Display — elegant serif for a premium feel
- **Body / UI**: Outfit — clean, modern sans-serif
- **Numbers / stats**: DM Mono — monospaced for score alignment

### Key CSS files
| File | Purpose |
|---|---|
| `wwwroot/app.css` | Global dark theme, Bootstrap overrides, CSS variables |
| `Components/Layout/MainLayout.razor.css` | Page layout, sidebar dimensions, top bar |
| `Components/Layout/NavMenu.razor.css` | Sidebar navigation, gold-tinted icons, active state |

## Changelog

### Latest
- **UI Redesign**: Full "Augusta Dark" theme — dark forest green palette, gold accents, Playfair Display headings, smooth page transition animations
- **Statistics**: Added Hole Difficulty Ranking table sorted by average score vs par
- **RoundService**: Streamlined HoleScore addition logic
- **Tests**: Unit tests added for HoleScore, Round, Course, RoundService, and StatisticsService

## License

MIT
