using System.ComponentModel.DataAnnotations;
using GolfScorer.Data;

namespace GolfScorer.Models;

public class Course
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Par { get; set; } = 72;

    public decimal? SlopeRating { get; set; }

    public decimal? CourseRating { get; set; }

    public int NumberOfHoles { get; set; } = 18;

    public bool IsPublic { get; set; } = false;

    // Par for each hole (stored as comma-separated values)
    public string HolePars { get; set; } = "4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4";

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public ICollection<Round> Rounds { get; set; } = new List<Round>();

    // Helper to get par for a specific hole
    public int GetHolePar(int holeNumber)
    {
        var pars = HolePars.Split(',');
        if (holeNumber >= 1 && holeNumber <= pars.Length)
        {
            return int.TryParse(pars[holeNumber - 1], out int par) ? par : 4;
        }
        return 4;
    }

    // Helper to set par for a specific hole
    public void SetHolePar(int holeNumber, int par)
    {
        var pars = HolePars.Split(',').ToList();
        while (pars.Count < holeNumber)
        {
            pars.Add("4");
        }
        pars[holeNumber - 1] = par.ToString();
        HolePars = string.Join(",", pars);
    }
}
