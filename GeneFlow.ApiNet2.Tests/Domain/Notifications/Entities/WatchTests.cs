using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Notifications.Events;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Domain.Notifications.Entities;

public class WatchTests
{
    [Fact]
    public void Create_ShouldRaiseWatchUpdatedEvent()
    {
        var watch = Watch.Create(new UserId(1), new StudyId(2), WatchLevel.All);

        watch.UserId.Should().Be(new UserId(1));
        watch.StudyId.Should().Be(new StudyId(2));
        watch.Level.Should().Be(WatchLevel.All);
        watch.DomainEvents.Should().ContainSingle(e => e is WatchUpdatedEvent);
    }

    [Fact]
    public void SetLevel_ToDifferentLevel_ShouldUpdateAndRaiseEvent()
    {
        var watch = Watch.Create(new UserId(1), new StudyId(2), WatchLevel.All);
        watch.ClearDomainEvents();

        watch.SetLevel(WatchLevel.None);

        watch.Level.Should().Be(WatchLevel.None);
        watch.ModifiedAt.Should().NotBeNull();
        watch.DomainEvents.Should().ContainSingle(e => e is WatchUpdatedEvent);
    }

    [Fact]
    public void SetLevel_ToSameLevel_ShouldBeNoOp()
    {
        var watch = Watch.Create(new UserId(1), new StudyId(2), WatchLevel.All);
        watch.ClearDomainEvents();

        watch.SetLevel(WatchLevel.All);

        watch.ModifiedAt.Should().BeNull();
        watch.DomainEvents.Should().BeEmpty();
    }
}
