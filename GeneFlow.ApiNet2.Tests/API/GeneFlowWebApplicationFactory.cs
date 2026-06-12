using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GeneFlow.ApiNet2.API;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Tests.API;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// </summary>
public class GeneFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    // Identity mocks
    public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();
    public IUserUnitOfWork MockUserUnitOfWork { get; } = Substitute.For<IUserUnitOfWork>();
    public IPasswordHasher MockPasswordHasher { get; } = Substitute.For<IPasswordHasher>();
    public IJwtTokenGenerator MockJwtTokenGenerator { get; } = Substitute.For<IJwtTokenGenerator>();
    public IEmailService MockEmailService { get; } = Substitute.For<IEmailService>();
    public ISequenceGenerator MockSequenceGenerator { get; } = Substitute.For<ISequenceGenerator>();
    public IUserAuthenticationValidator MockAuthValidator { get; } = Substitute.For<IUserAuthenticationValidator>();

    // Study mocks
    public IStudyRepository MockStudyRepository { get; } = Substitute.For<IStudyRepository>();
    public IStudyInvitationRepository MockStudyInvitationRepository { get; } = Substitute.For<IStudyInvitationRepository>();
    public IStudyUnitOfWork MockStudyUnitOfWork { get; } = Substitute.For<IStudyUnitOfWork>();

    // Subscription + plan mocks (used by SubscriptionLimitBehavior on study/member/invitation commands)
    public ISubscriptionRepository MockSubscriptionRepository { get; } = Substitute.For<ISubscriptionRepository>();
    public IPlanRepository MockPlanRepository { get; } = Substitute.For<IPlanRepository>();

    // Profile mock (used by study queries to enrich member info)
    public IProfileRepository MockProfileRepository { get; } = Substitute.For<IProfileRepository>();

    // Trace + storage mocks (used by export and trace endpoints)
    public ITraceRepository MockTraceRepository { get; } = Substitute.For<ITraceRepository>();
    public IFileStorageService MockFileStorageService { get; } = Substitute.For<IFileStorageService>();

    // Test user constants
    private const string TestUserId = "U00000001";
    private const string TestUserEmail = "testuser@example.com";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real DbContexts and add in-memory
            services.RemoveAll<DbContextOptions<UserContext>>();
            services.RemoveAll<UserContext>();
            services.AddDbContext<UserContext>(options =>
                options.UseInMemoryDatabase($"TestUserDb_{Guid.NewGuid()}"));

            services.RemoveAll<DbContextOptions<StudyContext>>();
            services.RemoveAll<StudyContext>();
            services.AddDbContext<StudyContext>(options =>
                options.UseInMemoryDatabase($"TestStudyDb_{Guid.NewGuid()}"));

            // Remove real Redis
            services.RemoveAll<IConnectionMultiplexer>();
            var mockMultiplexer = Substitute.For<IConnectionMultiplexer>();
            services.AddSingleton(mockMultiplexer);

            // Replace Identity services with mocks
            services.RemoveAll<IUserRepository>();
            services.AddScoped(_ => MockUserRepository);

            services.RemoveAll<IUserUnitOfWork>();
            services.AddScoped(_ => MockUserUnitOfWork);

            services.RemoveAll<IPasswordHasher>();
            services.AddScoped(_ => MockPasswordHasher);

            services.RemoveAll<IJwtTokenGenerator>();
            services.AddScoped(_ => MockJwtTokenGenerator);

            services.RemoveAll<IEmailService>();
            services.AddScoped(_ => MockEmailService);

            services.RemoveAll<ISequenceGenerator>();
            services.AddSingleton(_ => MockSequenceGenerator);

            services.RemoveAll<IUserAuthenticationValidator>();
            services.AddScoped(_ => MockAuthValidator);

            // Replace Study services with mocks
            services.RemoveAll<IStudyRepository>();
            services.AddScoped(_ => MockStudyRepository);

            services.RemoveAll<IStudyInvitationRepository>();
            services.AddScoped(_ => MockStudyInvitationRepository);

            services.RemoveAll<IStudyUnitOfWork>();
            services.AddScoped(_ => MockStudyUnitOfWork);

            services.RemoveAll<ISubscriptionRepository>();
            services.AddScoped(_ => MockSubscriptionRepository);

            services.RemoveAll<IPlanRepository>();
            services.AddScoped(_ => MockPlanRepository);

            services.RemoveAll<IProfileRepository>();
            services.AddScoped(_ => MockProfileRepository);

            services.RemoveAll<ITraceRepository>();
            services.AddScoped(_ => MockTraceRepository);

            services.RemoveAll<IFileStorageService>();
            services.AddSingleton(_ => MockFileStorageService);

            // Replace JWT authentication with test authentication
            // Remove existing authentication configuration
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }

    /// <summary>
    /// Creates an authenticated HTTP client for testing.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    /// <summary>
    /// Resets all mocks to their default state.
    /// </summary>
    public void ResetMocks()
    {
        // Identity mocks
        MockUserRepository.ClearReceivedCalls();
        MockUserUnitOfWork.ClearReceivedCalls();
        MockPasswordHasher.ClearReceivedCalls();
        MockJwtTokenGenerator.ClearReceivedCalls();
        MockEmailService.ClearReceivedCalls();
        MockSequenceGenerator.ClearReceivedCalls();
        MockAuthValidator.ClearReceivedCalls();

        // Study mocks
        MockStudyRepository.ClearReceivedCalls();
        MockStudyInvitationRepository.ClearReceivedCalls();
        MockStudyUnitOfWork.ClearReceivedCalls();

        // Subscription + plan mocks
        MockSubscriptionRepository.ClearReceivedCalls();
        MockPlanRepository.ClearReceivedCalls();

        // Profile mock
        MockProfileRepository.ClearReceivedCalls();

        // Trace + storage mocks
        MockTraceRepository.ClearReceivedCalls();
        MockFileStorageService.ClearReceivedCalls();

        // Setup default successful responses
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Default: Save changes succeeds
        MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
        MockStudyUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Default: User exists
        var testUser = CreateTestUser();
        MockUserRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(testUser);

        // Default: member enrichment in study queries resolves the test user, no profiles
        MockUserRepository
            .GetByIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<User>)new List<User> { testUser });
        MockProfileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Profile>)Array.Empty<Profile>());

        // Default: Sequence generation
        MockSequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        // Default: empty paged study lists (avoids NRE in handlers that don't stub the repo)
        var emptyStudies = PagedList<Study>.Create(new List<Study>(), 1, 10, 0);
        MockStudyRepository
            .GetPublishedAsync(
                Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string?>(), Arg.Any<ResearchField?>(),
                Arg.Any<IReadOnlyList<string>?>(), Arg.Any<string?>(),
                Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);
        MockStudyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<string?>(), Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);
        MockStudyRepository
            .GetFeaturedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Study>)Array.Empty<Study>());

        // Default: SubscriptionLimitBehavior — give the test user an unlimited free plan
        var planId = new PlanId(1);
        var planLimits = PlanLimits.CreateUnlimited();
        var planPricing = PlanPricing.Create(0m, 0m).Value;
        var planName = PlanName.Create("Free").Value;
        var plan = Plan.Create(planId, planName, "Free plan", planPricing, planLimits).Value;
        var subscription = Subscription.CreateFree(new SubscriptionId(1), new UserId(1), planId).Value;

        MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);
        MockPlanRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        // Default: no traces, missing files
        MockTraceRepository
            .GetByStudyIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Trace>)Array.Empty<Trace>());
        MockFileStorageService
            .GetFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);
    }

    /// <summary>Default no-trace export: study exists, member is owner, repo returns empty list.</summary>
    public void SetupExportStudySuccess(string studyId)
    {
        SetupStudyExists(studyId);
        MockTraceRepository
            .GetByStudyIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Trace>)Array.Empty<Trace>());
    }

    #region Study Setup Methods

    public void SetupStudyExists(string studyId)
    {
        var study = CreateTestStudy(studyId);
        MockStudyRepository
            .GetByIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetByIdWithMembersAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetMemberRoleAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(StudyRole.Owner);
        // SubscriptionLimitBehavior requires the study owner to resolve the
        // subscription that owns the limit being checked (member, trace, etc.).
        MockStudyRepository
            .GetOwnerIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(new UserId(1));
    }

    public void SetupStudyNotFound(string studyId)
    {
        MockStudyRepository
            .GetByIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns((Study?)null);
        MockStudyRepository
            .GetByIdWithMembersAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns((Study?)null);
        // Bypass StudyMembershipBehavior so the handler can return its real NotFound
        // result instead of the behavior short-circuiting to 403 Forbidden.
        MockStudyRepository
            .GetMemberRoleAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(StudyRole.Owner);
        MockStudyRepository
            .IsPublicStudyAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    public void SetupCreateStudySuccess()
    {
        MockSequenceGenerator
            .NextAsync(StudyId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);
        MockStudyRepository
            .AddAsync(Arg.Any<Study>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    public void SetupUpdateStudySuccess(string studyId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupChangeStatusSuccess(string studyId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupDeleteStudySuccess(string studyId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupDuplicateStudySuccess(string studyId)
    {
        SetupStudyExists(studyId);
        MockSequenceGenerator
            .NextAsync(StudyId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(2L);
    }

    #endregion

    #region Member Setup Methods

    public void SetupGetMembersSuccess(string studyId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupAddMemberSuccess(string studyId)
    {
        SetupStudyExists(studyId);
        MockUserRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(CreateTestUser());
    }

    public void SetupMemberAlreadyExists(string studyId, string userId)
    {
        // Study owned by auth user, with the target user already a member so AddMember returns Conflict.
        var memberNumeric = long.Parse(userId.Substring(1));
        var study = CreateTestStudyWithMember(studyId, new UserId(1), new UserId(memberNumeric), StudyRole.Editor);
        MockStudyRepository
            .GetByIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetByIdWithMembersAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetMemberRoleAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(StudyRole.Owner);
        MockStudyRepository
            .GetOwnerIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(new UserId(1));
    }

    public void SetupRemoveMemberSuccess(string studyId, string userId)
    {
        // Pre-populate the study with the target member so RemoveMember can find them.
        var memberNumeric = long.Parse(userId.Substring(1));
        var study = CreateTestStudyWithMember(studyId, new UserId(1), new UserId(memberNumeric), StudyRole.Editor);
        ApplyStudyMocks(studyId, study, ownerId: new UserId(1));
    }

    public void SetupMemberNotFound(string studyId, string userId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupChangeMemberRoleSuccess(string studyId, string userId)
    {
        // Pre-populate the study with the target member so ChangeMemberRole can find them.
        var memberNumeric = long.Parse(userId.Substring(1));
        var study = CreateTestStudyWithMember(studyId, new UserId(1), new UserId(memberNumeric), StudyRole.Editor);
        ApplyStudyMocks(studyId, study, ownerId: new UserId(1));
    }

    public void SetupLeaveStudySuccess(string studyId)
    {
        // Auth user (UserId 1) cannot leave if they're the owner; create a study owned
        // by a different user with the auth user as a regular Editor member.
        var ownerId = new UserId(2);
        var study = CreateTestStudyWithMember(studyId, ownerId, new UserId(1), StudyRole.Editor);
        ApplyStudyMocks(studyId, study, ownerId: ownerId);
    }

    public void SetupTransferOwnershipSuccess(string studyId, string newOwnerId)
    {
        // Pre-populate the study with the prospective new owner as Admin
        // (Study.TransferOwnership rejects non-Admin members; see StudyErrors.NewOwnerMustBeAdmin).
        var memberNumeric = long.Parse(newOwnerId.Substring(1));
        var study = CreateTestStudyWithMember(studyId, new UserId(1), new UserId(memberNumeric), StudyRole.Admin);
        ApplyStudyMocks(studyId, study, ownerId: new UserId(1));
    }

    #endregion

    #region Invitation Setup Methods

    public void SetupSendInvitationSuccess(string studyId)
    {
        SetupStudyExists(studyId);
        MockStudyInvitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        MockSequenceGenerator
            .NextAsync(StudyInvitationId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);
    }

    public void SetupInvitationAlreadyExists(string studyId, string email)
    {
        SetupStudyExists(studyId);
        MockStudyInvitationRepository
            .HasPendingInvitationAsync(
                Arg.Is<StudyId>(id => id.ToString() == studyId),
                Arg.Is<string>(e => e == email),
                Arg.Any<CancellationToken>())
            .Returns(true);
    }

    public void SetupGetInvitationsSuccess(string studyId)
    {
        SetupStudyExists(studyId);
        var emptyPage = PagedList<StudyInvitation>.Create(
            new List<StudyInvitation>(), 1, 10, 0);
        MockStudyInvitationRepository
            .GetByStudyAsync(Arg.Any<StudyId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    public void SetupCancelInvitationSuccess(string studyId, string invitationId)
    {
        SetupStudyExists(studyId);
        var invitation = CreateTestInvitation(invitationId, studyId);
        MockStudyInvitationRepository
            .GetByIdAsync(Arg.Is<StudyInvitationId>(id => id.ToString() == invitationId), Arg.Any<CancellationToken>())
            .Returns(invitation);
    }

    public void SetupInvitationNotFound(string studyId, string invitationId)
    {
        SetupStudyExists(studyId);
        MockStudyInvitationRepository
            .GetByIdAsync(Arg.Is<StudyInvitationId>(id => id.ToString() == invitationId), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);
    }

    public void SetupResendInvitationSuccess(string studyId, string invitationId)
    {
        SetupStudyExists(studyId);
        var invitation = CreateTestInvitation(invitationId, studyId);
        MockStudyInvitationRepository
            .GetByIdAsync(Arg.Is<StudyInvitationId>(id => id.ToString() == invitationId), Arg.Any<CancellationToken>())
            .Returns(invitation);
    }

    public void SetupGetMyInvitationsSuccess(string email)
    {
        var emptyPage = PagedList<StudyInvitation>.Create(
            new List<StudyInvitation>(), 1, 10, 0);
        MockStudyInvitationRepository
            .GetPendingByEmailAsync(Arg.Is<string>(e => e == email), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(emptyPage);
    }

    public void SetupGetInvitationByTokenSuccess(string token)
    {
        var invitation = CreateTestInvitation("I00000001", "S00000001");
        MockStudyInvitationRepository
            .GetByTokenAsync(Arg.Is<string>(t => t == token), Arg.Any<CancellationToken>())
            .Returns(invitation);
    }

    public void SetupInvitationTokenNotFound(string token)
    {
        MockStudyInvitationRepository
            .GetByTokenAsync(Arg.Is<string>(t => t == token), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);
    }

    public void SetupAcceptInvitationSuccess(string token)
    {
        // Invitation accepted by the auth user (UserId 1). The study must be owned by
        // someone else so adding the auth user as a member does not collide with the
        // existing owner-membership entry. The invitation's inviter is the owner
        // (UserId 2) so Study.AddMember's permission check finds them in _members.
        var ownerId = new UserId(2);
        var invitation = CreateTestInvitation("I00000001", "S00000001", invitedBy: ownerId);
        MockStudyInvitationRepository
            .GetByTokenAsync(Arg.Is<string>(t => t == token), Arg.Any<CancellationToken>())
            .Returns(invitation);

        var study = CreateTestStudy("S00000001", ownerId);
        ApplyStudyMocks("S00000001", study, ownerId: ownerId);
    }

    public void SetupDeclineInvitationSuccess(string token)
    {
        var invitation = CreateTestInvitation("I00000001", "S00000001");
        MockStudyInvitationRepository
            .GetByTokenAsync(Arg.Is<string>(t => t == token), Arg.Any<CancellationToken>())
            .Returns(invitation);
    }

    #endregion

    #region Paper Setup Methods

    public void SetupGetPapersSuccess(string studyId)
    {
        SetupStudyExists(studyId);
    }

    public void SetupAddPaperSuccess(string studyId)
    {
        SetupStudyExists(studyId);
        MockSequenceGenerator
            .NextAsync(StudyPaperId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);
    }

    public void SetupRemovePaperSuccess(string studyId, string paperId)
    {
        // Pre-populate the study with the target paper so RemovePaper can find it.
        var paperNumeric = long.Parse(paperId.Substring(1));
        var study = CreateTestStudyWithPaper(studyId, new UserId(1), new StudyPaperId(paperNumeric));
        ApplyStudyMocks(studyId, study, ownerId: new UserId(1));
    }

    public void SetupPaperNotFound(string studyId, string paperId)
    {
        SetupStudyExists(studyId);
        // Paper not found logic is handled in the study aggregate
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser()
    {
        var email = Email.Create(TestUserEmail).Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("hashed_password").Value;
        var userId = new UserId(1);

        return User.Create(userId, email, username, passwordHash).Value;
    }

    private static Study CreateTestStudy(string studyId, UserId? ownerId = null)
    {
        // Parse the study ID to get the numeric part
        var numericPart = long.Parse(studyId.Substring(1));
        var id = new StudyId(numericPart);
        var actualOwner = ownerId ?? new UserId(1);

        var title = StudyTitle.Create("Test Study Title").Value;
        var description = StudyDescription.Create("Test study description").Value;

        var studyResult = Study.Create(
            id,
            actualOwner,
            title,
            description,
            ResearchField.Genomics);

        return studyResult.Value;
    }

    private static Study CreateTestStudyWithMember(
        string studyId,
        UserId ownerId,
        UserId memberId,
        StudyRole memberRole)
    {
        var study = CreateTestStudy(studyId, ownerId);
        study.AddMember(memberId, memberRole, ownerId);
        return study;
    }

    private static Study CreateTestStudyWithPaper(
        string studyId,
        UserId ownerId,
        StudyPaperId paperId)
    {
        var study = CreateTestStudy(studyId, ownerId);
        var paper = StudyPaper.Create(
            id: paperId,
            title: "Test Paper",
            authors: "Test Author",
            doi: null,
            @abstract: null,
            journal: null,
            publicationYear: 2024,
            fileId: null,
            fileName: null,
            fileSizeBytes: null,
            uploadedBy: ownerId).Value;
        study.AddPaper(paper, ownerId);
        return study;
    }

    /// <summary>
    /// Wires every per-study mock — GetByIdAsync, GetByIdWithMembersAsync,
    /// GetMemberRoleAsync, GetOwnerIdAsync — to point to the supplied study
    /// instance so commands that load via different APIs see the same state.
    /// </summary>
    private void ApplyStudyMocks(string studyId, Study study, UserId ownerId)
    {
        MockStudyRepository
            .GetByIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetByIdWithMembersAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(study);
        MockStudyRepository
            .GetMemberRoleAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(StudyRole.Owner);
        MockStudyRepository
            .GetOwnerIdAsync(Arg.Is<StudyId>(id => id.ToString() == studyId), Arg.Any<CancellationToken>())
            .Returns(ownerId);
    }

    private static StudyInvitation CreateTestInvitation(
        string invitationId,
        string studyId,
        UserId? invitedBy = null)
    {
        var numericPart = long.Parse(invitationId.Substring(1));
        var id = new StudyInvitationId(numericPart);
        var studyNumericPart = long.Parse(studyId.Substring(1));
        var studyIdParsed = new StudyId(studyNumericPart);
        var invitedById = invitedBy ?? new UserId(1);

        var invitationResult = StudyInvitation.Create(
            id,
            studyIdParsed,
            "invitee@example.com",
            StudyRole.Editor,
            invitedById,
            null);

        return invitationResult.Value;
    }

    #endregion
}

/// <summary>
/// Test authentication handler for integration tests.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no Authorization header, allow anonymous access (for public endpoints)
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "U00000001"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "testuser@example.com"),
            new Claim("sub", "U00000001")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
