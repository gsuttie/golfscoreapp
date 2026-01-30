using GolfScorer.Models;
using GolfScorer.Services;
using GolfScorer.Tests.Helpers;
using Xunit;

namespace GolfScorer.Tests.Services;

public class RoundServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private static Course CreateTestCourse(string userId = UserId) => new()
    {
        UserId = userId,
        Name = "Test Course",
        Par = 72,
        SlopeRating = 125.0m,
        CourseRating = 72.0m
    };

    private static List<HoleScore> CreateHoleScores(int baseScore = 4, int basePutts = 2)
    {
        return Enumerable.Range(1, 18).Select(h => new HoleScore
        {
            HoleNumber = h,
            Par = 4,
            Score = baseScore,
            Putts = basePutts,
            GreenInRegulation = baseScore <= 4,
            FairwayHit = h <= 14 ? true : null // par 3s (last 4) get null
        }).ToList();
    }

    [Fact]
    public async Task CreateRoundAsync_CalculatesDenormalizedTotals()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var holeScores = CreateHoleScores(baseScore: 5, basePutts: 2);
        // Make a few holes different
        holeScores[0].Score = 3;  // birdie
        holeScores[0].GreenInRegulation = true;
        holeScores[1].Score = 4;  // par
        holeScores[1].Putts = 1;
        holeScores[1].GreenInRegulation = true;

        var round = new Round
        {
            UserId = UserId,
            CourseId = course.Id,
            DatePlayed = DateTime.Today
        };

        var created = await roundService.CreateRoundAsync(round, holeScores);

        // 3 + 4 + (16 * 5) = 87
        Assert.Equal(87, created.TotalScore);
        // 2 + 1 + (16 * 2) = 35
        Assert.Equal(35, created.TotalPutts);
        // Only holes 0 and 1 have GreenInRegulation = true (rest are baseScore 5 > 4)
        Assert.Equal(2, created.GIRCount);
        // First 14 holes have FairwayHit = true, last 4 null
        Assert.Equal(14, created.FairwaysHit);
    }

    [Fact]
    public async Task GetRoundsAsync_FilteredByYear()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2024, 6, 1) },
            CreateHoleScores());

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2025, 3, 15) },
            CreateHoleScores());

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2025, 7, 20) },
            CreateHoleScores());

        var all = await roundService.GetRoundsAsync(UserId);
        Assert.Equal(3, all.Count);

        var year2025 = await roundService.GetRoundsAsync(UserId, 2025);
        Assert.Equal(2, year2025.Count);

        var year2024 = await roundService.GetRoundsAsync(UserId, 2024);
        Assert.Single(year2024);
    }

    [Fact]
    public async Task GetRoundAsync_ReturnsNullForUnauthorizedAccess()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            CreateHoleScores());

        var result = await roundService.GetRoundAsync(round.Id, OtherUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoundAsync_ReturnsRoundWithCourseAndHoleScores()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            CreateHoleScores());

        var result = await roundService.GetRoundAsync(round.Id, UserId);

        Assert.NotNull(result);
        Assert.NotNull(result.Course);
        Assert.Equal("Test Course", result.Course.Name);
        Assert.Equal(18, result.HoleScores.Count);
        // Hole scores should be ordered by HoleNumber
        Assert.Equal(1, result.HoleScores.First().HoleNumber);
        Assert.Equal(18, result.HoleScores.Last().HoleNumber);
    }

    [Fact]
    public async Task UpdateRoundAsync_UpdatesMetadataProperties()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round
            {
                UserId = UserId,
                CourseId = course.Id,
                WeatherConditions = "Sunny",
                Notes = "Good round"
            },
            CreateHoleScores());

        var updated = await roundService.UpdateRoundAsync(
            new Round
            {
                Id = round.Id,
                CourseId = course.Id,
                DatePlayed = new DateTime(2025, 6, 15),
                WeatherConditions = "Rainy",
                Notes = "Tough conditions"
            },
            CreateHoleScores(),
            UserId);

        Assert.NotNull(updated);
        Assert.Equal(new DateTime(2025, 6, 15), updated.DatePlayed);
        Assert.Equal("Rainy", updated.WeatherConditions);
        Assert.Equal("Tough conditions", updated.Notes);
    }

    [Fact]
    public async Task GetRoundsAsync_OrderedByDateDescending()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2025, 1, 1) },
            CreateHoleScores());
        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2025, 6, 1) },
            CreateHoleScores());
        await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id, DatePlayed = new DateTime(2025, 3, 1) },
            CreateHoleScores());

        var rounds = await roundService.GetRoundsAsync(UserId);

        Assert.Equal(new DateTime(2025, 6, 1), rounds[0].DatePlayed);
        Assert.Equal(new DateTime(2025, 3, 1), rounds[1].DatePlayed);
        Assert.Equal(new DateTime(2025, 1, 1), rounds[2].DatePlayed);
    }

    [Fact]
    public async Task UpdateRoundAsync_ReplacesHoleScoresAndRecalculatesTotals()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var originalScores = CreateHoleScores(baseScore: 5, basePutts: 2);
        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            originalScores);

        Assert.Equal(90, round.TotalScore); // 18 * 5

        // Update with better scores
        var newScores = CreateHoleScores(baseScore: 4, basePutts: 2);
        var updated = await roundService.UpdateRoundAsync(
            new Round { Id = round.Id, CourseId = course.Id, DatePlayed = DateTime.Today },
            newScores,
            UserId);

        Assert.NotNull(updated);
        Assert.Equal(72, updated.TotalScore); // 18 * 4
    }

    [Fact]
    public async Task UpdateRoundAsync_ReturnsNullForUnauthorizedAccess()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            CreateHoleScores());

        var result = await roundService.UpdateRoundAsync(
            new Round { Id = round.Id, CourseId = course.Id },
            CreateHoleScores(),
            OtherUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteRoundAsync_RemovesRoundAndHoleScores()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            CreateHoleScores());

        var deleted = await roundService.DeleteRoundAsync(round.Id, UserId);
        Assert.True(deleted);

        var result = await roundService.GetRoundAsync(round.Id, UserId);
        Assert.Null(result);

        // Hole scores should also be removed
        Assert.Empty(context.HoleScores.Where(h => h.RoundId == round.Id));
    }

    [Fact]
    public async Task DeleteRoundAsync_ReturnsFalseForUnauthorizedAccess()
    {
        using var context = TestDbContextFactory.Create();
        var courseService = new CourseService(context);
        var roundService = new RoundService(context);

        var course = await courseService.CreateCourseAsync(CreateTestCourse());

        var round = await roundService.CreateRoundAsync(
            new Round { UserId = UserId, CourseId = course.Id },
            CreateHoleScores());

        var result = await roundService.DeleteRoundAsync(round.Id, OtherUserId);
        Assert.False(result);
    }
}
