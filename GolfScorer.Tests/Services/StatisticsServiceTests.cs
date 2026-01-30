using GolfScorer.Models;
using GolfScorer.Services;
using GolfScorer.Tests.Helpers;
using Xunit;

namespace GolfScorer.Tests.Services;

public class StatisticsServiceTests
{
    private const string UserId = "user-1";

    private static Course CreateTestCourse(decimal? slopeRating = 125.0m, decimal? courseRating = 72.0m) => new()
    {
        UserId = UserId,
        Name = "Test Course",
        Par = 72,
        SlopeRating = slopeRating,
        CourseRating = courseRating
    };

    private static List<HoleScore> CreateHoleScores(
        int baseScore = 4,
        int basePutts = 2,
        bool gir = true,
        bool? fairwayHit = true)
    {
        return Enumerable.Range(1, 18).Select(h => new HoleScore
        {
            HoleNumber = h,
            Par = 4,
            Score = baseScore,
            Putts = basePutts,
            GreenInRegulation = gir,
            FairwayHit = h <= 14 ? fairwayHit : null
        }).ToList();
    }

    private static async Task<Round> SeedRound(
        CourseService courseService,
        RoundService roundService,
        Course course,
        DateTime date,
        int baseScore = 4,
        int basePutts = 2)
    {
        return await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = date },
            CreateHoleScores(baseScore, basePutts));
    }

    [Fact]
    public async Task GetStatisticsAsync_NoRounds_ReturnsZeroedStats()
    {
        using var context = TestDbContextFactory.Create();
        var service = new StatisticsService(context);

        var stats = await service.GetStatisticsAsync(UserId);

        Assert.Equal(0, stats.TotalRounds);
        Assert.Equal(0, stats.AverageScore);
        Assert.Equal(0, stats.BestScore);
        Assert.Equal(0, stats.WorstScore);
        Assert.Null(stats.HandicapIndex);
        Assert.Empty(stats.RecentRounds);
        Assert.Empty(stats.HoleAverages);
    }

    [Fact]
    public async Task GetStatisticsAsync_CalculatesCorrectAverages()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // Round 1: all 4s (par) = 72 total, 36 putts
        await SeedRound(courseService, roundService, course, new DateTime(2025, 1, 1), baseScore: 4, basePutts: 2);
        // Round 2: all 5s (bogey) = 90 total, 54 putts
        await SeedRound(courseService, roundService, course, new DateTime(2025, 2, 1), baseScore: 5, basePutts: 3);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(2, stats.TotalRounds);
        Assert.Equal(81, stats.AverageScore);    // (72 + 90) / 2
        Assert.Equal(72, stats.BestScore);
        Assert.Equal(90, stats.WorstScore);
        Assert.Equal(45, stats.AveragePutts);     // (36 + 54) / 2
    }

    [Fact]
    public async Task GetStatisticsAsync_ScoreDistribution()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // Create a round with mixed scores
        var holeScores = new List<HoleScore>
        {
            new() { HoleNumber = 1, Par = 4, Score = 2, Putts = 1, GreenInRegulation = true, FairwayHit = true },  // Eagle
            new() { HoleNumber = 2, Par = 4, Score = 3, Putts = 1, GreenInRegulation = true, FairwayHit = true },  // Birdie
            new() { HoleNumber = 3, Par = 4, Score = 4, Putts = 2, GreenInRegulation = true, FairwayHit = true },  // Par
            new() { HoleNumber = 4, Par = 4, Score = 5, Putts = 2, GreenInRegulation = false, FairwayHit = true }, // Bogey
            new() { HoleNumber = 5, Par = 4, Score = 6, Putts = 2, GreenInRegulation = false, FairwayHit = false },// Double
            new() { HoleNumber = 6, Par = 4, Score = 7, Putts = 3, GreenInRegulation = false, FairwayHit = false },// Triple
        };
        // Fill remaining holes with pars
        for (int h = 7; h <= 18; h++)
        {
            holeScores.Add(new HoleScore
            {
                HoleNumber = h,
                Par = 4,
                Score = 4,
                Putts = 2,
                GreenInRegulation = true,
                FairwayHit = h <= 14 ? true : null
            });
        }

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = DateTime.Today },
            holeScores);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(1, stats.EagleOrBetterCount);
        Assert.Equal(1, stats.BirdieCount);
        Assert.Equal(13, stats.ParCount); // hole 3 + holes 7-18
        Assert.Equal(1, stats.BogeyCount);
        Assert.Equal(1, stats.DoubleBogeyCount);
        Assert.Equal(1, stats.TripleOrWorseCount);
    }

    [Fact]
    public async Task GetAvailableYearsAsync_ReturnsCorrectYearList()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        await SeedRound(courseService, roundService, course, new DateTime(2023, 5, 1));
        await SeedRound(courseService, roundService, course, new DateTime(2024, 3, 15));
        await SeedRound(courseService, roundService, course, new DateTime(2024, 8, 20));
        await SeedRound(courseService, roundService, course, new DateTime(2025, 1, 10));

        var years = await statsService.GetAvailableYearsAsync(UserId);

        Assert.Equal(3, years.Count);
        Assert.Equal([2025, 2024, 2023], years); // descending order
    }

    [Fact]
    public async Task GetStatisticsAsync_HandicapIndexWithSufficientRounds()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse(slopeRating: 113.0m, courseRating: 72.0m));

        // Create 5 rounds, all scoring 80 on a course with slope 113 and rating 72
        // Differential = (80 - 72) * 113 / 113 = 8.0
        for (int i = 0; i < 5; i++)
        {
            // 18 holes scoring 4 each = 72 total, but we need 80
            // Use scores that sum to 80: 16 holes at 4 + 2 holes at 6 = 64 + 12 = 76...
            // Let's make 2 holes score 5 and 16 holes score 4: 2*5 + 16*4 = 10+64=74. Still not 80.
            // 8 holes at 5, 10 holes at 4: 40+40=80
            var scores = Enumerable.Range(1, 18).Select(h => new HoleScore
            {
                HoleNumber = h,
                Par = 4,
                Score = h <= 8 ? 5 : 4,
                Putts = 2,
                GreenInRegulation = h > 8,
                FairwayHit = h <= 14 ? true : null
            }).ToList();

            await roundService.CreateRoundAsync(
                new Round { UserId = UserId, CourseId = course.Id, DatePlayed = DateTime.Today.AddDays(-i) },
                scores);
        }

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.NotNull(stats.HandicapIndex);
        // With slope=113, rating=72, score=80: differential = (80-72)*113/113 = 8.0
        // 5 rounds => take best 2 (5/2=2) => average of best 2 = 8.0
        Assert.Equal(8.0, stats.HandicapIndex);
    }

    [Fact]
    public async Task GetStatisticsAsync_GIRAndFairwayPercentages()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // 18 holes: 9 GIR, 7 fairway-applicable holes with 4 hit
        var holeScores = Enumerable.Range(1, 18).Select(h => new HoleScore
        {
            HoleNumber = h,
            Par = h <= 4 ? 3 : 4, // first 4 are par 3 (no fairway)
            Score = 4,
            Putts = 2,
            GreenInRegulation = h <= 9, // first 9 are GIR
            FairwayHit = h <= 4 ? null : (h <= 8 ? true : false) // holes 5-8 hit, 9-18 missed
        }).ToList();

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = DateTime.Today },
            holeScores);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(50.0, stats.GIRPercentage); // 9/18
        // Fairway-applicable holes: 5-18 = 14 holes. Hit: 5-8 = 4. Percentage = 4/14 * 100
        Assert.Equal(Math.Round(4.0 * 100 / 14, 1), stats.FairwayPercentage);
    }

    [Fact]
    public async Task GetStatisticsAsync_PuttingPercentages()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // 18 holes: 4 one-putts, 10 two-putts, 3 three-putts, 1 four-putt
        var holeScores = Enumerable.Range(1, 18).Select(h => new HoleScore
        {
            HoleNumber = h,
            Par = 4,
            Score = 4,
            Putts = h <= 4 ? 1 : (h <= 14 ? 2 : (h <= 17 ? 3 : 4)),
            GreenInRegulation = true,
            FairwayHit = h <= 14 ? true : null
        }).ToList();

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = DateTime.Today },
            holeScores);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(Math.Round(4.0 * 100 / 18, 1), stats.OnePuttPercentage);
        Assert.Equal(Math.Round(10.0 * 100 / 18, 1), stats.TwoPuttPercentage);
        // ThreePuttPercentage includes 3+ putts (3 three-putts + 1 four-putt = 4)
        Assert.Equal(Math.Round(4.0 * 100 / 18, 1), stats.ThreePuttPercentage);
    }

    [Fact]
    public async Task GetStatisticsAsync_ParAverages()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // Mix of par 3s, 4s, 5s with specific scores
        var holeScores = new List<HoleScore>
        {
            // Par 3s: scores 3, 4 => avg 3.5
            new() { HoleNumber = 1, Par = 3, Score = 3, Putts = 2, GreenInRegulation = true, FairwayHit = null },
            new() { HoleNumber = 2, Par = 3, Score = 4, Putts = 2, GreenInRegulation = false, FairwayHit = null },
            // Par 4s: scores 4, 5, 5 => avg 4.67
            new() { HoleNumber = 3, Par = 4, Score = 4, Putts = 2, GreenInRegulation = true, FairwayHit = true },
            new() { HoleNumber = 4, Par = 4, Score = 5, Putts = 2, GreenInRegulation = false, FairwayHit = true },
            new() { HoleNumber = 5, Par = 4, Score = 5, Putts = 2, GreenInRegulation = false, FairwayHit = false },
            // Par 5s: scores 5, 6 => avg 5.5
            new() { HoleNumber = 6, Par = 5, Score = 5, Putts = 2, GreenInRegulation = true, FairwayHit = true },
            new() { HoleNumber = 7, Par = 5, Score = 6, Putts = 3, GreenInRegulation = false, FairwayHit = false },
        };
        // Fill remaining with par 4s scoring 4
        for (int h = 8; h <= 18; h++)
            holeScores.Add(new HoleScore
            {
                HoleNumber = h, Par = 4, Score = 4, Putts = 2,
                GreenInRegulation = true, FairwayHit = h <= 14 ? true : null
            });

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = DateTime.Today },
            holeScores);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(3.5, stats.Par3Average);
        Assert.Equal(Math.Round((4 + 5 + 5 + 4.0 * 11) / 14, 2), stats.Par4Average); // 3 explicit + 11 filler par 4s
        Assert.Equal(5.5, stats.Par5Average);
    }

    [Fact]
    public async Task GetStatisticsAsync_FilteredByYear()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // 2024 round: score 90
        await SeedRound(courseService, roundService, course, new DateTime(2024, 6, 1), baseScore: 5);
        // 2025 rounds: score 72 each
        await SeedRound(courseService, roundService, course, new DateTime(2025, 3, 1), baseScore: 4);
        await SeedRound(courseService, roundService, course, new DateTime(2025, 7, 1), baseScore: 4);

        var stats2025 = await statsService.GetStatisticsAsync(UserId, 2025);

        Assert.Equal(2, stats2025.TotalRounds);
        Assert.Equal(72, stats2025.AverageScore);
        Assert.Equal(72, stats2025.BestScore);
    }

    [Fact]
    public async Task GetStatisticsAsync_RecentRoundsLimitedTo10()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        for (int i = 0; i < 15; i++)
            await SeedRound(courseService, roundService, course, DateTime.Today.AddDays(-i));

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(15, stats.TotalRounds);
        Assert.Equal(10, stats.RecentRounds.Count);
    }

    [Fact]
    public async Task GetStatisticsAsync_HoleAveragesPopulated()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        await SeedRound(courseService, roundService, course, DateTime.Today, baseScore: 5);

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(18, stats.HoleAverages.Count);
        Assert.All(stats.HoleAverages, ha =>
        {
            Assert.Equal(5.0, ha.AverageScore);
            Assert.Equal(4.0, ha.AveragePar);
            Assert.Equal(1.0, ha.AverageVsPar);
        });
    }

    [Fact]
    public async Task GetStatisticsAsync_HandicapNullWhenCoursesMissingRatings()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        // Course with no slope/course ratings
        var course = await courseService.CreateCourseAsync(
            CreateTestCourse(slopeRating: null, courseRating: null));

        for (int i = 0; i < 5; i++)
            await SeedRound(courseService, roundService, course, DateTime.Today.AddDays(-i));

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(5, stats.TotalRounds);
        Assert.Null(stats.HandicapIndex);
    }

    [Fact]
    public async Task GetAvailableYearsAsync_NoRounds_ReturnsEmptyList()
    {
        using var context = TestDbContextFactory.Create();
        var statsService = new StatisticsService(context);

        var years = await statsService.GetAvailableYearsAsync(UserId);

        Assert.Empty(years);
    }

    [Fact]
    public async Task GetStatisticsAsync_HandicapIndexNullWithFewerThan3Rounds()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);
        var statsService = new StatisticsService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        // Only 2 rounds - below minimum of 3
        await SeedRound(courseService, roundService, course, new DateTime(2025, 1, 1));
        await SeedRound(courseService, roundService, course, new DateTime(2025, 2, 1));

        var stats = await statsService.GetStatisticsAsync(UserId);

        Assert.Equal(2, stats.TotalRounds);
        Assert.Null(stats.HandicapIndex);
    }
}
