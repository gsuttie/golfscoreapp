using Microsoft.EntityFrameworkCore;
using GolfScorer.Data;

namespace GolfScorer.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create([System.Runtime.CompilerServices.CallerMemberName] string? testName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"{testName}_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
