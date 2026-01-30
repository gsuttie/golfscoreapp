using GolfScorer.Models;
using Xunit;

namespace GolfScorer.Tests.Models;

public class CourseTests
{
    [Fact]
    public void GetHolePar_ReturnsCorrectPar()
    {
        var course = new Course { HolePars = "3,4,5,4,4,3,4,5,4,4,3,4,5,4,4,3,4,5" };

        Assert.Equal(3, course.GetHolePar(1));
        Assert.Equal(4, course.GetHolePar(2));
        Assert.Equal(5, course.GetHolePar(3));
    }

    [Fact]
    public void GetHolePar_OutOfRange_ReturnsDefault4()
    {
        var course = new Course();

        Assert.Equal(4, course.GetHolePar(0));
        Assert.Equal(4, course.GetHolePar(19));
        Assert.Equal(4, course.GetHolePar(-1));
    }

    [Fact]
    public void SetHolePar_UpdatesExistingHole()
    {
        var course = new Course();

        course.SetHolePar(1, 3);
        Assert.Equal(3, course.GetHolePar(1));

        course.SetHolePar(5, 5);
        Assert.Equal(5, course.GetHolePar(5));
    }

    [Fact]
    public void SetHolePar_PadsMissingHoles()
    {
        var course = new Course { HolePars = "4,4,4" };

        course.SetHolePar(6, 5);

        Assert.Equal(4, course.GetHolePar(4)); // padded
        Assert.Equal(4, course.GetHolePar(5)); // padded
        Assert.Equal(5, course.GetHolePar(6)); // set
    }
}
