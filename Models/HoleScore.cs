using System.ComponentModel.DataAnnotations;

namespace GolfScorer.Models;

public class HoleScore
{
    public int Id { get; set; }

    [Required]
    public int RoundId { get; set; }

    [Required]
    [Range(1, 18)]
    public int HoleNumber { get; set; }

    [Required]
    [Range(3, 5)]
    public int Par { get; set; } = 4;

    [Required]
    [Range(1, 15)]
    public int Score { get; set; }

    [Required]
    [Range(0, 10)]
    public int Putts { get; set; }

    public bool GreenInRegulation { get; set; }

    // Nullable for par 3s where there's no fairway
    public bool? FairwayHit { get; set; }

    // Navigation property
    public Round? Round { get; set; }

    // Calculated properties
    public int ScoreToPar => Score - Par;

    public string ScoreToParDisplay
    {
        get
        {
            var diff = ScoreToPar;
            return diff switch
            {
                <= -3 => "Albatross",
                -2 => "Eagle",
                -1 => "Birdie",
                0 => "Par",
                1 => "Bogey",
                2 => "Double",
                3 => "Triple",
                _ => $"+{diff}"
            };
        }
    }
}
