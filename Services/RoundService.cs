using Microsoft.EntityFrameworkCore;
using GolfScorer.Data;
using GolfScorer.Models;

namespace GolfScorer.Services;

public class RoundService
{
    private readonly ApplicationDbContext _context;

    public RoundService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Round>> GetRoundsAsync(string userId, int? year = null)
    {
        var query = _context.Rounds
            .Include(r => r.Course)
            .Where(r => r.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(r => r.DatePlayed.Year == year.Value);
        }

        return await query
            .OrderByDescending(r => r.DatePlayed)
            .ToListAsync();
    }

    public async Task<Round?> GetRoundAsync(int id, string userId)
    {
        return await _context.Rounds
            .Include(r => r.Course)
            .Include(r => r.HoleScores.OrderBy(h => h.HoleNumber))
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task<Round> CreateRoundAsync(Round round, List<HoleScore> holeScores)
    {
        // Calculate totals
        round.TotalScore = holeScores.Sum(h => h.Score);
        round.TotalPutts = holeScores.Sum(h => h.Putts);
        round.GIRCount = holeScores.Count(h => h.GreenInRegulation);
        round.FairwaysHit = holeScores.Count(h => h.FairwayHit == true);

        _context.Rounds.Add(round);
        await _context.SaveChangesAsync();

        foreach (var score in holeScores)
        {
            score.RoundId = round.Id;
            _context.HoleScores.Add(score);
        }

        await _context.SaveChangesAsync();
        return round;
    }

    public async Task<Round?> UpdateRoundAsync(Round round, List<HoleScore> holeScores, string userId)
    {
        var existing = await _context.Rounds
            .Include(r => r.HoleScores)
            .FirstOrDefaultAsync(r => r.Id == round.Id && r.UserId == userId);

        if (existing == null)
            return null;

        existing.CourseId = round.CourseId;
        existing.DatePlayed = round.DatePlayed;
        existing.WeatherConditions = round.WeatherConditions;
        existing.Notes = round.Notes;

        // Update totals
        existing.TotalScore = holeScores.Sum(h => h.Score);
        existing.TotalPutts = holeScores.Sum(h => h.Putts);
        existing.GIRCount = holeScores.Count(h => h.GreenInRegulation);
        existing.FairwaysHit = holeScores.Count(h => h.FairwayHit == true);

        // Remove existing hole scores and add new ones
        _context.HoleScores.RemoveRange(existing.HoleScores);

        foreach (var score in holeScores)
        {
            _context.HoleScores.Add(new HoleScore
            {
                RoundId = existing.Id,
                HoleNumber = score.HoleNumber,
                Par = score.Par,
                Score = score.Score,
                Putts = score.Putts,
                GreenInRegulation = score.GreenInRegulation,
                FairwayHit = score.FairwayHit
            });
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteRoundAsync(int id, string userId)
    {
        var round = await _context.Rounds
            .Include(r => r.HoleScores)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (round == null)
            return false;

        _context.HoleScores.RemoveRange(round.HoleScores);
        _context.Rounds.Remove(round);
        await _context.SaveChangesAsync();
        return true;
    }
}
