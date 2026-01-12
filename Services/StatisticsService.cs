using Microsoft.EntityFrameworkCore;
using GolfScorer.Data;
using GolfScorer.Models;

namespace GolfScorer.Services;

public class StatisticsService
{
    private readonly ApplicationDbContext _context;

    public StatisticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GolfStatistics> GetStatisticsAsync(string userId, int? year = null)
    {
        var query = _context.Rounds
            .Include(r => r.Course)
            .Include(r => r.HoleScores)
            .Where(r => r.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Year == year.Value);
        }

        var rounds = await query.ToListAsync();

        if (!rounds.Any())
        {
            return new GolfStatistics();
        }

        var allHoleScores = rounds.SelectMany(r => r.HoleScores).ToList();

        var stats = new GolfStatistics
        {
            TotalRounds = rounds.Count,
            AverageScore = Math.Round(rounds.Average(r => r.TotalScore), 1),
            BestScore = rounds.Min(r => r.TotalScore),
            WorstScore = rounds.Max(r => r.TotalScore),
            AveragePutts = Math.Round(rounds.Average(r => r.TotalPutts), 1),
            GIRPercentage = Math.Round(allHoleScores.Count(h => h.GreenInRegulation) * 100.0 / allHoleScores.Count, 1),
            FairwayPercentage = CalculateFairwayPercentage(allHoleScores),

            // Par performance
            Par3Average = CalculateParAverage(allHoleScores, 3),
            Par4Average = CalculateParAverage(allHoleScores, 4),
            Par5Average = CalculateParAverage(allHoleScores, 5),

            // Putting stats
            OnePuttPercentage = CalculatePuttPercentage(allHoleScores, 1),
            TwoPuttPercentage = CalculatePuttPercentage(allHoleScores, 2),
            ThreePuttPercentage = CalculatePuttPercentage(allHoleScores, 3, includeAbove: true),

            // Score distribution
            EagleOrBetterCount = allHoleScores.Count(h => h.ScoreToPar <= -2),
            BirdieCount = allHoleScores.Count(h => h.ScoreToPar == -1),
            ParCount = allHoleScores.Count(h => h.ScoreToPar == 0),
            BogeyCount = allHoleScores.Count(h => h.ScoreToPar == 1),
            DoubleBogeyCount = allHoleScores.Count(h => h.ScoreToPar == 2),
            TripleOrWorseCount = allHoleScores.Count(h => h.ScoreToPar >= 3),

            // Recent rounds for trend
            RecentRounds = rounds
                .OrderByDescending(r => r.DatePlayed)
                .Take(10)
                .Select(r => new RoundSummary
                {
                    Date = r.DatePlayed,
                    CourseName = r.Course?.Name ?? "Unknown",
                    Score = r.TotalScore,
                    Par = r.Course?.Par ?? 72,
                    Putts = r.TotalPutts,
                    GIRs = r.GIRCount
                })
                .ToList(),

            // Handicap calculation (simplified differential)
            HandicapIndex = CalculateHandicapIndex(rounds)
        };

        // Per-hole analysis
        stats.HoleAverages = Enumerable.Range(1, 18)
            .Select(hole =>
            {
                var holeScores = allHoleScores.Where(h => h.HoleNumber == hole).ToList();
                return new HoleAverage
                {
                    HoleNumber = hole,
                    AverageScore = holeScores.Any() ? Math.Round(holeScores.Average(h => h.Score), 2) : 0,
                    AveragePar = holeScores.Any() ? Math.Round(holeScores.Average(h => (double)h.Par), 2) : 4,
                    GIRPercentage = holeScores.Any() ? Math.Round(holeScores.Count(h => h.GreenInRegulation) * 100.0 / holeScores.Count, 1) : 0
                };
            })
            .ToList();

        return stats;
    }

    private double CalculateFairwayPercentage(List<HoleScore> holeScores)
    {
        var fairwayHoles = holeScores.Where(h => h.FairwayHit.HasValue).ToList();
        if (!fairwayHoles.Any()) return 0;
        return Math.Round(fairwayHoles.Count(h => h.FairwayHit == true) * 100.0 / fairwayHoles.Count, 1);
    }

    private double CalculateParAverage(List<HoleScore> holeScores, int par)
    {
        var parHoles = holeScores.Where(h => h.Par == par).ToList();
        if (!parHoles.Any()) return par;
        return Math.Round(parHoles.Average(h => h.Score), 2);
    }

    private double CalculatePuttPercentage(List<HoleScore> holeScores, int puttCount, bool includeAbove = false)
    {
        if (!holeScores.Any()) return 0;

        var count = includeAbove
            ? holeScores.Count(h => h.Putts >= puttCount)
            : holeScores.Count(h => h.Putts == puttCount);

        return Math.Round(count * 100.0 / holeScores.Count, 1);
    }

    private double? CalculateHandicapIndex(List<Round> rounds)
    {
        if (rounds.Count < 3) return null;

        // Use most recent 20 rounds, take best 8 differentials
        var recentRounds = rounds
            .Where(r => r.Course != null && r.Course.SlopeRating.HasValue && r.Course.CourseRating.HasValue)
            .OrderByDescending(r => r.DatePlayed)
            .Take(20)
            .ToList();

        if (recentRounds.Count < 3) return null;

        var differentials = recentRounds
            .Select(r =>
            {
                var slope = (double)r.Course!.SlopeRating!.Value;
                var rating = (double)r.Course!.CourseRating!.Value;
                return (r.TotalScore - rating) * 113 / slope;
            })
            .OrderBy(d => d)
            .Take(Math.Max(1, recentRounds.Count / 2))
            .ToList();

        return Math.Round(differentials.Average(), 1);
    }

    public async Task<List<int>> GetAvailableYearsAsync(string userId)
    {
        return await _context.Rounds
            .Where(r => r.UserId == userId)
            .Select(r => r.DatePlayed.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }
}

public class GolfStatistics
{
    public int TotalRounds { get; set; }
    public double AverageScore { get; set; }
    public int BestScore { get; set; }
    public int WorstScore { get; set; }
    public double AveragePutts { get; set; }
    public double GIRPercentage { get; set; }
    public double FairwayPercentage { get; set; }
    public double Par3Average { get; set; }
    public double Par4Average { get; set; }
    public double Par5Average { get; set; }
    public double OnePuttPercentage { get; set; }
    public double TwoPuttPercentage { get; set; }
    public double ThreePuttPercentage { get; set; }
    public int EagleOrBetterCount { get; set; }
    public int BirdieCount { get; set; }
    public int ParCount { get; set; }
    public int BogeyCount { get; set; }
    public int DoubleBogeyCount { get; set; }
    public int TripleOrWorseCount { get; set; }
    public double? HandicapIndex { get; set; }
    public List<RoundSummary> RecentRounds { get; set; } = new();
    public List<HoleAverage> HoleAverages { get; set; } = new();
}

public class RoundSummary
{
    public DateTime Date { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Par { get; set; }
    public int Putts { get; set; }
    public int GIRs { get; set; }
    public int ScoreToPar => Score - Par;
}

public class HoleAverage
{
    public int HoleNumber { get; set; }
    public double AverageScore { get; set; }
    public double AveragePar { get; set; }
    public double GIRPercentage { get; set; }
    public double AverageVsPar => Math.Round(AverageScore - AveragePar, 2);
}
