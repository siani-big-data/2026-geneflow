using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Tests.Domain.Discussions.Entities;

public class ReactionTests
{
    [Fact]
    public void Create_WithValidEmoji_ShouldSucceed()
    {
        var result = Reaction.Create(Guid.NewGuid(), new UserId(1), "👍");

        result.IsSuccess.Should().BeTrue();
        result.Value.Emoji.Should().Be("👍");
        result.Value.DomainEvents.Should().ContainSingle(e => e is ReactionAddedEvent);
    }

    [Fact]
    public void Create_WithEmptyEmoji_ShouldFail()
    {
        var result = Reaction.Create(Guid.NewGuid(), new UserId(1), "");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReactionErrors.EmojiRequired);
    }

    [Fact]
    public void Create_WithTooLongEmoji_ShouldFail()
    {
        var tooLong = new string('x', Reaction.MaxEmojiLength + 1);
        var result = Reaction.Create(Guid.NewGuid(), new UserId(1), tooLong);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReactionErrors.EmojiTooLong);
    }

    [Fact]
    public void MarkRemoved_ShouldRaiseReactionRemovedEvent()
    {
        var reaction = Reaction.Create(Guid.NewGuid(), new UserId(1), "👍").Value;
        reaction.ClearDomainEvents();

        reaction.MarkRemoved();

        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionRemovedEvent);
    }
}
