using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Tests.Domain.Discussions.Entities;

public class CommentTests
{
    private static UserId NewUserId(long v = 1) => new(v);

    private static Comment NewComment(UserId? author = null) =>
        Comment.Create(
            CommentParentType.Discussion,
            "D00000001",
            author ?? NewUserId(1),
            "Hello world").Value;

    [Fact]
    public void Create_WithValidBody_ShouldSucceed()
    {
        var c = Comment.Create(CommentParentType.Discussion, "D00000001", NewUserId(1), "Body");

        c.IsSuccess.Should().BeTrue();
        c.Value.BodyMarkdown.Should().Be("Body");
        c.Value.IsDeleted.Should().BeFalse();
        c.Value.EditedAt.Should().BeNull();
        c.Value.DomainEvents.Should().ContainSingle(e => e is CommentCreatedEvent);
    }

    [Fact]
    public void Create_WithEmptyBody_ShouldFail()
    {
        var c = Comment.Create(CommentParentType.Discussion, "D00000001", NewUserId(1), "   ");

        c.IsFailure.Should().BeTrue();
        c.Error.Should().Be(CommentErrors.BodyRequired);
    }

    [Fact]
    public void Edit_ByAuthor_ShouldUpdateBodyAndRaiseEvent()
    {
        var comment = NewComment(NewUserId(1));
        comment.ClearDomainEvents();

        var result = comment.Edit(NewUserId(1), "Updated");

        result.IsSuccess.Should().BeTrue();
        comment.BodyMarkdown.Should().Be("Updated");
        comment.EditedAt.Should().NotBeNull();
        comment.DomainEvents.Should().ContainSingle(e => e is CommentEditedEvent);
    }

    [Fact]
    public void Edit_ByNonAuthor_ShouldFail()
    {
        var comment = NewComment(NewUserId(1));

        var result = comment.Edit(NewUserId(2), "hack");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CommentErrors.NotAuthor);
        comment.BodyMarkdown.Should().Be("Hello world");
    }

    [Fact]
    public void Edit_OnDeleted_ShouldFail()
    {
        var comment = NewComment(NewUserId(1));
        comment.Delete(NewUserId(1), isAdmin: false);

        var result = comment.Edit(NewUserId(1), "x");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CommentErrors.AlreadyDeleted);
    }

    [Fact]
    public void Delete_ByAuthor_ShouldSoftDeleteAndRaise()
    {
        var comment = NewComment(NewUserId(1));
        comment.ClearDomainEvents();

        var result = comment.Delete(NewUserId(1), isAdmin: false);

        result.IsSuccess.Should().BeTrue();
        comment.IsDeleted.Should().BeTrue();
        comment.DeletedAt.Should().NotBeNull();
        comment.DomainEvents.Should().ContainSingle(e => e is CommentDeletedEvent);
    }

    [Fact]
    public void Delete_ByAdmin_ShouldSucceed_EvenWhenNotAuthor()
    {
        var comment = NewComment(NewUserId(1));

        var result = comment.Delete(NewUserId(99), isAdmin: true);

        result.IsSuccess.Should().BeTrue();
        comment.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Delete_ByNonAuthor_NonAdmin_ShouldFail()
    {
        var comment = NewComment(NewUserId(1));

        var result = comment.Delete(NewUserId(2), isAdmin: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CommentErrors.NotAuthor);
        comment.IsDeleted.Should().BeFalse();
    }
}
