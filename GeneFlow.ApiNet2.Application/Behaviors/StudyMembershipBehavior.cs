using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates study membership for requests
/// that implement <see cref="IRequireStudyMembership"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class StudyMembershipBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireStudyMembership
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStudyRepository _studyRepository;

    public StudyMembershipBehavior(
        ICurrentUserService currentUserService,
        IStudyRepository studyRepository)
    {
        _currentUserService = currentUserService;
        _studyRepository = studyRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Authentication is already validated by AuthenticationBehavior (IRequireStudyMembership extends IRequireAuthentication)
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return CreateForbiddenResult("User not authenticated.");
        }

        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
        {
            return CreateForbiddenResult("Invalid study ID.");
        }

        // Get the user's role in the study
        var memberRole = await _studyRepository.GetMemberRoleAsync(
            studyId,
            userId,
            cancellationToken);

        // If not a member, check if it's a public study (for read-only operations)
        if (memberRole is null)
        {
            // Only allow access to public studies if no minimum role is required
            if (request.MinimumRole is null)
            {
                var isPublic = await _studyRepository.IsPublicStudyAsync(studyId, cancellationToken);
                if (isPublic)
                {
                    return await next();
                }
            }

            // A study that no longer exists (or was soft-deleted) has no owner.
            // Return NotFound instead of Forbidden so deleted resources don't
            // appear to still exist behind an access check.
            var ownerId = await _studyRepository.GetOwnerIdAsync(studyId, cancellationToken);
            if (ownerId is null)
            {
                return CreateErrorResult(Error.NotFound("Study.NotFound", "Study was not found."));
            }

            return CreateForbiddenResult("User is not a member of this study.");
        }

        // Check if the user has the minimum required role
        if (request.MinimumRole is not null)
        {
            if (!HasSufficientRole(memberRole, request.MinimumRole))
            {
                return CreateForbiddenResult(
                    $"This operation requires {request.MinimumRole.Name} role or higher.");
            }
        }

        return await next();
    }

    /// <summary>
    /// Checks if the user's role meets or exceeds the minimum required role.
    /// Role hierarchy: Owner > Admin > Editor > Viewer
    /// </summary>
    private static bool HasSufficientRole(StudyRole userRole, StudyRole minimumRole)
    {
        // Lower ID = higher privilege (Owner=1, Admin=2, Editor=3, Viewer=4)
        return userRole.Id <= minimumRole.Id;
    }

    private static TResponse CreateForbiddenResult(string message)
        => CreateErrorResult(Error.Forbidden("Study.AccessDenied", message));

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
            $"StudyMembershipBehavior does not support response type {typeof(TResponse).Name}. " +
            "Only Result and Result<T> are supported.");
    }
}
