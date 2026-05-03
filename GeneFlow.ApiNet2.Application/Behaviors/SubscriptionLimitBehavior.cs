using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Subscriptions;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates subscription limits before processing commands.
/// Checks MaxStudies, MaxTracesPerMonth, and MaxMembersPerStudy based on the user's plan.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class SubscriptionLimitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ITraceRepository _traceRepository;

    public SubscriptionLimitBehavior(
        ICurrentUserService currentUserService,
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IStudyRepository studyRepository,
        ITraceRepository traceRepository)
    {
        _currentUserService = currentUserService;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _studyRepository = studyRepository;
        _traceRepository = traceRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Check if this request requires any limit validation
        var requiresStudyLimit = request is IRequiresStudyLimit;
        var requiresTraceLimit = request is IRequiresTraceLimit;
        var requiresMemberLimit = request is IRequiresMemberLimit;

        if (!requiresStudyLimit && !requiresTraceLimit && !requiresMemberLimit)
        {
            return await next();
        }

        // Determine whose subscription to check
        UserId userId;

        if (requiresStudyLimit)
        {
            // For study creation, check the current user's subscription
            if (_currentUserService.UserId is null)
            {
                return CreateErrorResult(Error.Unauthorized("User.NotAuthenticated", "User must be authenticated."));
            }

            if (!UserId.TryParse(_currentUserService.UserId, out var parsedUserId) || parsedUserId is null)
            {
                return CreateErrorResult(Error.Validation("User.InvalidId", "Invalid user ID."));
            }
            userId = parsedUserId;
        }
        else
        {
            // For trace/member limits, check the study owner's subscription
            var studyIdString = request switch
            {
                IRequiresTraceLimit traceLimit => traceLimit.StudyId,
                IRequiresMemberLimit memberLimit => memberLimit.StudyId,
                _ => throw new InvalidOperationException("Request must implement IRequiresTraceLimit or IRequiresMemberLimit")
            };

            if (!StudyId.TryParse(studyIdString, out var studyId) || studyId is null)
            {
                return CreateErrorResult(Error.Validation("Study.InvalidId", "Invalid study ID."));
            }

            var ownerId = await _studyRepository.GetOwnerIdAsync(studyId, cancellationToken);
            if (ownerId is null)
            {
                return CreateErrorResult(Error.NotFound("Study.NotFound", "Study not found."));
            }
            userId = ownerId;
        }

        // Get the user's active subscription
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (subscription is null)
        {
            return CreateErrorResult(SubscriptionLimitErrors.NoActiveSubscription);
        }

        // Get the plan limits
        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
        {
            return CreateErrorResult(Error.NotFound("Plan.NotFound", "Subscription plan not found."));
        }

        // Validate the specific limit
        if (requiresStudyLimit)
        {
            var result = await ValidateStudyLimitAsync(userId, plan, cancellationToken);
            if (result.IsFailure)
            {
                return CreateErrorResult(result.Error);
            }
        }

        if (requiresTraceLimit)
        {
            var traceRequest = (IRequiresTraceLimit)request;
            if (!StudyId.TryParse(traceRequest.StudyId, out var studyId) || studyId is null)
            {
                return CreateErrorResult(Error.Validation("Study.InvalidId", "Invalid study ID."));
            }

            var result = await ValidateTraceLimitAsync(studyId, traceRequest.TraceCount, plan, cancellationToken);
            if (result.IsFailure)
            {
                return CreateErrorResult(result.Error);
            }
        }

        if (requiresMemberLimit)
        {
            var memberRequest = (IRequiresMemberLimit)request;
            if (!StudyId.TryParse(memberRequest.StudyId, out var studyId) || studyId is null)
            {
                return CreateErrorResult(Error.Validation("Study.InvalidId", "Invalid study ID."));
            }

            var result = await ValidateMemberLimitAsync(studyId, plan, cancellationToken);
            if (result.IsFailure)
            {
                return CreateErrorResult(result.Error);
            }
        }

        return await next();
    }

    private async Task<Result> ValidateStudyLimitAsync(UserId userId, Plan plan, CancellationToken cancellationToken)
    {
        var maxStudies = plan.Limits.MaxStudies;

        // -1 means unlimited
        if (maxStudies == -1)
        {
            return Result.Success();
        }

        var currentCount = await _studyRepository.CountByOwnerIdAsync(userId, cancellationToken);

        if (currentCount >= maxStudies)
        {
            return Result.Failure(SubscriptionLimitErrors.StudyLimitReached(maxStudies, plan.Name.Value));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateTraceLimitAsync(StudyId studyId, int traceCount, Plan plan, CancellationToken cancellationToken)
    {
        var maxTraces = plan.Limits.MaxTracesPerMonth;

        // -1 means unlimited
        if (maxTraces == -1)
        {
            return Result.Success();
        }

        var currentCount = await _traceRepository.CountByStudyInCurrentMonthAsync(studyId, cancellationToken);

        if (currentCount + traceCount > maxTraces)
        {
            var remaining = Math.Max(0, maxTraces - currentCount);
            return Result.Failure(SubscriptionLimitErrors.TraceLimitReached(maxTraces, remaining, plan.Name.Value));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateMemberLimitAsync(StudyId studyId, Plan plan, CancellationToken cancellationToken)
    {
        var maxMembers = plan.Limits.MaxMembersPerStudy;

        // -1 means unlimited
        if (maxMembers == -1)
        {
            return Result.Success();
        }

        var currentCount = await _studyRepository.CountMembersAsync(studyId, cancellationToken);

        if (currentCount >= maxMembers)
        {
            return Result.Failure(SubscriptionLimitErrors.MemberLimitReached(maxMembers, plan.Name.Value));
        }

        return Result.Success();
    }

    private static TResponse CreateErrorResult(Error error)
    {
        // Handle Result<TValue> responses
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.Failure), new[] { typeof(Error) });

            return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
        }

        // Handle plain Result responses
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        throw new InvalidOperationException(
            $"SubscriptionLimitBehavior does not support response type {typeof(TResponse).Name}. " +
            "Only Result and Result<T> are supported.");
    }
}

/// <summary>
/// Error definitions for subscription limit violations.
/// </summary>
public static class SubscriptionLimitErrors
{
    public static readonly Error NoActiveSubscription = Error.Forbidden(
        "Subscription.NoActiveSubscription",
        "You don't have an active subscription. Please subscribe to continue.");

    public static Error StudyLimitReached(int maxStudies, string planName) => Error.Forbidden(
        "Subscription.StudyLimitReached",
        $"You have reached the maximum number of studies ({maxStudies}) allowed by your {planName} plan. " +
        "Please upgrade your plan to create more studies.");

    public static Error TraceLimitReached(int maxTraces, int remaining, string planName) => Error.Forbidden(
        "Subscription.TraceLimitReached",
        $"You have reached the monthly trace limit ({maxTraces}) for your {planName} plan. " +
        $"Remaining traces this month: {remaining}. Please upgrade your plan or wait until next month.");

    public static Error MemberLimitReached(int maxMembers, string planName) => Error.Forbidden(
        "Subscription.MemberLimitReached",
        $"This study has reached the maximum number of members ({maxMembers}) allowed by the owner's {planName} plan. " +
        "The study owner needs to upgrade their plan to add more members.");
}
