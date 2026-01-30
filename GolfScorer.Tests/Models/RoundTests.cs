using GolfScorer.Models;
using Xunit;

namespace GolfScorer.Tests.Models;

public class RoundTests
{
    [Fact]
    public void CalculateTotals_SumsCorrectly()
    {
        var round = new Round
        {
            HoleScores = new List<HoleScore>
            {
                new() { Score = 4, Putts = 2, GreenInRegulation = true, FairwayHit = true },
                new() { Score = 5, Putts = 3, GreenInRegulation = false, FairwayHit = false },
                new() { Score = 3, Putts = 1, GreenInRegulation = true, FairwayHit = null },
            }
        };

        round.CalculateTotals();

        Assert.Equal(12, round.TotalScore);
        Assert.Equal(6, round.TotalPutts);
        Assert.Equal(2, round.GIRCount);
        Assert.Equal(1, round.FairwaysHit);
    }

    [Fact]
    public void ScoreToPar_WithCourse_ReturnsCorrectDiff()
    {
        var round = new Round
        {
            TotalScore = 80,
            Course = new Course { Par = 72 }
        };

        Assert.Equal(8, round.ScoreToPar);
    }

    [Fact]
    public void ScoreToPar_WithoutCourse_ReturnsZero()
    {
        var round = new Round { TotalScore = 80, Course = null };

        Assert.Equal(0, round.ScoreToPar);
    }
}
