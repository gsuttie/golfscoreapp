using Microsoft.EntityFrameworkCore;
using GolfScorer.Data;
using GolfScorer.Models;

namespace GolfScorer.Services;

public class CourseService
{
    private readonly ApplicationDbContext _context;

    public CourseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Course>> GetCoursesAsync(string userId)
    {
        return await _context.Courses
            .Where(c => c.UserId == userId || c.IsPublic)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Course>> GetUserCoursesAsync(string userId)
    {
        return await _context.Courses
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseAsync(int id, string userId)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.IsPublic));
    }

    public async Task<Course> CreateCourseAsync(Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task<Course?> UpdateCourseAsync(Course course, string userId)
    {
        var existing = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == course.Id && c.UserId == userId);

        if (existing == null)
            return null;

        existing.Name = course.Name;
        existing.Par = course.Par;
        existing.SlopeRating = course.SlopeRating;
        existing.CourseRating = course.CourseRating;
        existing.NumberOfHoles = course.NumberOfHoles;
        existing.IsPublic = course.IsPublic;
        existing.HolePars = course.HolePars;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteCourseAsync(int id, string userId)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
            return false;

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
        return true;
    }
}
