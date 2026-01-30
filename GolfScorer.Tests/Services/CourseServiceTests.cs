using GolfScorer.Models;
using GolfScorer.Services;
using GolfScorer.Tests.Helpers;
using Xunit;

namespace GolfScorer.Tests.Services;

public class CourseServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    [Fact]
    public async Task CreateCourseAsync_And_GetCourseAsync_ReturnsCourse()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = new Course
        {
            UserId = UserId,
            Name = "St Andrews",
            Par = 72,
            SlopeRating = 125.0m,
            CourseRating = 72.0m
        };

        var created = await service.CreateCourseAsync(course);

        Assert.True(created.Id > 0);

        var retrieved = await service.GetCourseAsync(created.Id, UserId);

        Assert.NotNull(retrieved);
        Assert.Equal("St Andrews", retrieved.Name);
        Assert.Equal(72, retrieved.Par);
    }

    [Fact]
    public async Task GetCoursesAsync_ReturnsUserOwnedAndPublicCourses()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        await service.CreateCourseAsync(new Course { UserId = UserId, Name = "My Course" });
        await service.CreateCourseAsync(new Course { UserId = OtherUserId, Name = "Other Private", IsPublic = false });
        await service.CreateCourseAsync(new Course { UserId = OtherUserId, Name = "Public Course", IsPublic = true });

        var courses = await service.GetCoursesAsync(UserId);

        Assert.Equal(2, courses.Count);
        Assert.Contains(courses, c => c.Name == "My Course");
        Assert.Contains(courses, c => c.Name == "Public Course");
        Assert.DoesNotContain(courses, c => c.Name == "Other Private");
    }

    [Fact]
    public async Task GetCourseAsync_ReturnsNullForUnauthorizedAccess()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = await service.CreateCourseAsync(new Course
        {
            UserId = OtherUserId,
            Name = "Private Course",
            IsPublic = false
        });

        var result = await service.GetCourseAsync(course.Id, UserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCourseAsync_ReturnsPublicCourseForAnyUser()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = await service.CreateCourseAsync(new Course
        {
            UserId = OtherUserId,
            Name = "Public Course",
            IsPublic = true
        });

        var result = await service.GetCourseAsync(course.Id, UserId);

        Assert.NotNull(result);
        Assert.Equal("Public Course", result.Name);
    }

    [Fact]
    public async Task UpdateCourseAsync_RespectsOwnership()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = await service.CreateCourseAsync(new Course
        {
            UserId = UserId,
            Name = "Original Name",
            Par = 72
        });

        // Owner can update
        var updated = await service.UpdateCourseAsync(
            new Course { Id = course.Id, Name = "Updated Name", Par = 71, HolePars = course.HolePars },
            UserId);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(71, updated.Par);

        // Non-owner cannot update
        var unauthorized = await service.UpdateCourseAsync(
            new Course { Id = course.Id, Name = "Hacked", Par = 99, HolePars = course.HolePars },
            OtherUserId);
        Assert.Null(unauthorized);
    }

    [Fact]
    public async Task GetUserCoursesAsync_ReturnsOnlyOwnedCourses()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        await service.CreateCourseAsync(new Course { UserId = UserId, Name = "My Course" });
        await service.CreateCourseAsync(new Course { UserId = OtherUserId, Name = "Public Course", IsPublic = true });
        await service.CreateCourseAsync(new Course { UserId = OtherUserId, Name = "Other Private" });

        var courses = await service.GetUserCoursesAsync(UserId);

        Assert.Single(courses);
        Assert.Equal("My Course", courses[0].Name);
    }

    [Fact]
    public async Task UpdateCourseAsync_UpdatesAllProperties()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = await service.CreateCourseAsync(new Course
        {
            UserId = UserId,
            Name = "Original",
            Par = 72,
            NumberOfHoles = 18,
            IsPublic = false,
            SlopeRating = 113.0m,
            CourseRating = 70.0m,
            HolePars = "4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4"
        });

        var updated = await service.UpdateCourseAsync(new Course
        {
            Id = course.Id,
            Name = "Updated",
            Par = 70,
            NumberOfHoles = 9,
            IsPublic = true,
            SlopeRating = 125.0m,
            CourseRating = 72.5m,
            HolePars = "3,4,5,4,4,3,4,5,4"
        }, UserId);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(70, updated.Par);
        Assert.Equal(9, updated.NumberOfHoles);
        Assert.True(updated.IsPublic);
        Assert.Equal(125.0m, updated.SlopeRating);
        Assert.Equal(72.5m, updated.CourseRating);
        Assert.Equal("3,4,5,4,4,3,4,5,4", updated.HolePars);
    }

    [Fact]
    public async Task DeleteCourseAsync_RespectsOwnership()
    {
        using var context = TestDbContextFactory.Create();
        var service = new CourseService(context);

        var course = await service.CreateCourseAsync(new Course
        {
            UserId = UserId,
            Name = "To Delete"
        });

        // Non-owner cannot delete
        var denied = await service.DeleteCourseAsync(course.Id, OtherUserId);
        Assert.False(denied);

        // Owner can delete
        var deleted = await service.DeleteCourseAsync(course.Id, UserId);
        Assert.True(deleted);

        // Course is gone
        var result = await service.GetCourseAsync(course.Id, UserId);
        Assert.Null(result);
    }
}
