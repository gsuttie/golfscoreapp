using GolfScorer.Models;
using Xunit;

namespace GolfScorer.Tests.Models;

public class HoleScoreTests
{
    [Theory]
    [InlineData(4, 1, -3, "Albatross")]
    [InlineData(4, 2, -2, "Eagle")]
    [InlineData(5, 3, -2, "Eagle")]
    [InlineData(4, 3, -1, "Birdie")]
    [InlineData(4, 4, 0, "Par")]
    [InlineData(4, 5, 1, "Bogey")]
    [InlineData(4, 6, 2, "Double")]
    [InlineData(4, 7, 3, "Triple")]
    [InlineData(4, 8, 4, "+4")]
    [InlineData(4, 9, 5, "+5")]
    public void ScoreToParDisplay_ReturnsCorrectLabel(int par, int score, int expectedDiff, string expectedDisplay)
    {
        var hole = new HoleScore { Par = par, Score = score };

        Assert.Equal(expectedDiff, hole.ScoreToPar);
        Assert.Equal(expectedDisplay, hole.ScoreToParDisplay);
    }
}
