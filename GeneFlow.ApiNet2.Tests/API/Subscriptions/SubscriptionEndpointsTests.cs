using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API;
using GeneFlow.ApiNet2.API.Contracts.Subscriptions.Requests;
using GeneFlow.ApiNet2.API.Contracts.Subscriptions.Responses;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NSubstitute.ReturnsExtensions;

namespace GeneFlow.ApiNet2.Tests.API.Subscriptions;

/// <summary>
/// Integration tests for subscription endpoints.
/// </summary>
public class SubscriptionEndpointsTests : IClassFixture<SubscriptionWebApplicationFactory>, IDisposable
{
    private readonly SubscriptionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SubscriptionEndpointsTests(SubscriptionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetMocks();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private static long _subscriptionSequence = 1;
    private static long _planSequence = 1;

    private static Subscription CreateTestSubscription(
        long userId = 1,
        string planName = "Pro",
        bool startWithTrial = false)
    {
        return Subscription.Create(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(userId),
            PlanId.FromSequence(_planSequence++),
            planName,
            BillingCycle.Monthly,
            startWithTrial).Value;
    }

    private static Subscription CreateFreeSubscription(long userId = 1)
    {
        return Subscription.CreateFree(
            SubscriptionId.FromSequence(_subscriptionSequence++),
            new UserId(userId),
            PlanId.FromSequence(_planSequence++)).Value;
    }

    private void SetupAuthenticatedUser(long userId = 1)
    {
        var userIdObj = new UserId(userId);
        _factory.MockCurrentUserService.UserId.Returns(userIdObj);
        _factory.MockCurrentUserService.IsAuthenticated.Returns(true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
    }

    private void SetupUnauthenticatedUser()
    {
        _factory.MockCurrentUserService.UserId.ReturnsNull();
        _factory.MockCurrentUserService.IsAuthenticated.Returns(false);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region GetCurrentSubscription Tests

    [Fact]
    public async Task GetCurrentSubscription_WithValidAuth_ShouldReturn200()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription = CreateTestSubscription();

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        content.Should().NotBeNull();
        content!.PlanName.Should().Be("Pro");
        content.Status.Should().Be("Active");
        content.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentSubscription_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentSubscription_WhenNoSubscription_ShouldReturn404()
    {
        // Arrange
        SetupAuthenticatedUser();

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCurrentSubscription_WithFreeSubscription_ShouldReturn200()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription = CreateFreeSubscription();

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        content.Should().NotBeNull();
        content!.IsFree.Should().BeTrue();
        content.PlanName.Should().Be("Free");
    }

    #endregion

    #region GetSubscriptionHistory Tests

    [Fact]
    public async Task GetSubscriptionHistory_WithValidAuth_ShouldReturn200()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription1 = CreateTestSubscription(planName: "Basic");
        var subscription2 = CreateTestSubscription(planName: "Pro");
        var subscriptions = new List<Subscription> { subscription1, subscription2 };

        _factory.MockSubscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscriptions);

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<SubscriptionSummaryResponse>>();
        content.Should().NotBeNull();
        content.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithEmptyHistory_ShouldReturn200WithEmptyList()
    {
        // Arrange
        SetupAuthenticatedUser();

        _factory.MockSubscriptionRepository
            .GetAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subscription>());

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<SubscriptionSummaryResponse>>();
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    #endregion

    #region CancelSubscription Tests

    [Fact]
    public async Task CancelSubscription_WithValidAuth_ShouldReturn204()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription = CreateTestSubscription();
        var request = new CancelSubscriptionRequest("Switching provider");

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _factory.MockSubscriptionUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CancelSubscription_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new CancelSubscriptionRequest("No longer needed");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelSubscription_WhenNoSubscription_ShouldReturn404()
    {
        // Arrange
        SetupAuthenticatedUser();
        var request = new CancelSubscriptionRequest();

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelSubscription_WhenAlreadyExpired_ShouldReturn400()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription = CreateTestSubscription();
        subscription.Expire();
        var request = new CancelSubscriptionRequest();

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ChangePlan Tests

    [Fact]
    public async Task ChangePlan_Upgrade_ShouldReturn200()
    {
        // Arrange
        SetupAuthenticatedUser();
        var subscription = CreateTestSubscription(planName: "Basic");
        var newPlanId = PlanId.FromSequence(99);
        var plan = CreateTestPlan(newPlanId, "Pro", true);
        var request = new ChangePlanRequest(newPlanId.ToString(), 1);

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(subscription);

        _factory.MockPlanRepository
            .GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        _factory.MockSubscriptionUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/change-plan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePlan_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new ChangePlanRequest("P00000001", 1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/change-plan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePlan_WhenNoSubscription_ShouldReturn404()
    {
        // Arrange
        SetupAuthenticatedUser();
        var request = new ChangePlanRequest("P00000001", 1);

        _factory.MockSubscriptionRepository
            .GetActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/change-plan", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region CreateSubscription Tests - Note: Requires plan setup

    [Fact]
    public async Task CreateSubscription_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var request = new CreateSubscriptionRequest(Guid.NewGuid(), 1, false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper - Plan Creation

    private static Plan CreateTestPlan(PlanId planId, string name, bool isActive)
    {
        // Using the correct Plan.Create signature
        return Plan.Create(
            planId,
            PlanName.Create(name).Value,
            "Test plan description",
            PlanPricing.Create(9.99m, 99.99m).Value,
            PlanLimits.Create(100, 10, 5).Value).Value;
    }

    #endregion
}

/// <summary>
/// Custom WebApplicationFactory for subscription integration tests.
/// </summary>
public class SubscriptionWebApplicationFactory : WebApplicationFactory<Program>
{
    public ISubscriptionRepository MockSubscriptionRepository { get; } = Substitute.For<ISubscriptionRepository>();
    public ISubscriptionUnitOfWork MockSubscriptionUnitOfWork { get; } = Substitute.For<ISubscriptionUnitOfWork>();
    public IPlanRepository MockPlanRepository { get; } = Substitute.For<IPlanRepository>();
    public ICurrentUserService MockCurrentUserService { get; } = Substitute.For<ICurrentUserService>();
    public ISequenceGenerator MockSequenceGenerator { get; } = Substitute.For<ISequenceGenerator>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove real services and add mocks
            services.RemoveAll<ISubscriptionRepository>();
            services.AddScoped(_ => MockSubscriptionRepository);

            services.RemoveAll<ISubscriptionUnitOfWork>();
            services.AddScoped(_ => MockSubscriptionUnitOfWork);

            services.RemoveAll<IPlanRepository>();
            services.AddScoped(_ => MockPlanRepository);

            services.RemoveAll<ICurrentUserService>();
            services.AddScoped(_ => MockCurrentUserService);

            services.RemoveAll<ISequenceGenerator>();
            services.AddSingleton(_ => MockSequenceGenerator);

            // Replace JWT authentication with the shared TestAuthHandler so endpoints
            // that require [Authorize] don't return 401 in integration tests.
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.RemoveAll<IPostConfigureOptions<AuthenticationOptions>>();

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    /// <summary>
    /// Creates an authenticated HTTP client for testing.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
        return client;
    }

    /// <summary>
    /// Resets all mocks to their default state.
    /// </summary>
    public void ResetMocks()
    {
        MockSubscriptionRepository.ClearReceivedCalls();
        MockSubscriptionUnitOfWork.ClearReceivedCalls();
        MockPlanRepository.ClearReceivedCalls();
        MockCurrentUserService.ClearReceivedCalls();
        MockSequenceGenerator.ClearReceivedCalls();
    }
}
