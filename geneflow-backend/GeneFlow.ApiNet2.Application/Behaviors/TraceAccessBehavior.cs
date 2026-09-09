using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using MediatR;

namespace GeneFlow.ApiNet2.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates trace access for requests
/// that implement <see cref="IRequireTraceAccess"/>.
/// Looks up the trace's parent study and validates membership.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class TraceAccessBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequireTraceAccess
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITraceRepository _traceRepository;
    private readonly IStudyRepository _studyRepository;

    public TraceAccessBehavior(
        ICurrentUserService currentUserService,
        ITraceRepository traceRepository,
        IStudyRepository studyRepository)
    {
        _currentUserService = currentUserService;
        _traceRepository = traceRepository;
        _studyRepository = studyRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Authentication is already validated by AuthenticationBehavior (IRequireTraceAccess extends IRequireAuthentication)
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return CreateForbiddenResult("User not authenticated.");
        }

        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
        {
            return CreateForbiddenResult("Invalid trace ID.");
        }

        // Get the study ID for this trace
        var studyId = await _traceRepository.GetStudyIdAsync(traceId, cancellationToken);
        if (studyId is null)
        {
            return CreateNotFoundResult("Trace not found.");
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

            return CreateForbiddenResult("User is not a member of the study containing this trace.");
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
    {
        var error = Error.Forbidden("Trace.AccessDenied", message);
        return CreateErrorResult(error);
    }

    private static TResponse CreateNotFoundResult(string message)
    {
        var error = Error.NotFound("Trace.NotFound", message);
        return CreateErrorResult(error);
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
            $"TraceAccessBehavior does not support response type {typeof(TResponse).Name}. " +
            "Only Result and Result<T> are supported.");
    }
}
