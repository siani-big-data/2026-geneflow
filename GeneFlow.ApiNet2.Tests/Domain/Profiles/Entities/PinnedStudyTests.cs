using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles.Entities;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.Entities;

/// <summary>
/// Unit tests for the PinnedStudy entity invariants.
/// </summary>
public class PinnedStudyTests
{
    [Fact]
    public void Create_ValidOrder_ShouldSucceed()
    {
        var pin = PinnedStudy.Create(new UserId(1), new StudyId(1), 0);

        pin.UserId.Should().Be(new UserId(1));
        pin.StudyId.Should().Be(new StudyId(1));
        pin.Order.Should().Be(0);
        pin.PinnedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_OrderWithinRange_ShouldSucceed(int order)
    {
        var act = () => PinnedStudy.Create(new UserId(1), new StudyId(1), order);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    public void Create_OrderOutOfRange_ShouldThrow(int order)
    {
        var act = () => PinnedStudy.Create(new UserId(1), new StudyId(1), order);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MaxPinnedPerUser_ShouldBeSix()
    {
        PinnedStudy.MaxPinnedPerUser.Should().Be(6);
    }
}
