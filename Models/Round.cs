using System.ComponentModel.DataAnnotations;
using GolfScorer.Data;

namespace GolfScorer.Models;

public class Round
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public DateTime DatePlayed { get; set; } = DateTime.Today;

    [StringLength(50)]
    public string? WeatherConditions { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Calculated totals (stored for quick queries)
    public int TotalScore { get; set; }
    public int TotalPutts { get; set; }
    public int GIRCount { get; set; }
    public int FairwaysHit { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public Course? Course { get; set; }
    public ICollection<HoleScore> HoleScores { get; set; } = new List<HoleScore>();

    // Calculated properties
    public int ScoreToPar => Course != null ? TotalScore - Course.Par : 0;

    public void CalculateTotals()
    {
        TotalScore = HoleScores.Sum(h => h.Score);
        TotalPutts = HoleScores.Sum(h => h.Putts);
        GIRCount = HoleScores.Count(h => h.GreenInRegulation);
        FairwaysHit = HoleScores.Count(h => h.FairwayHit == true);
    }
}
