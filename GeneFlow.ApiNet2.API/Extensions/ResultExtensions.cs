using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Extension methods for converting Result to IResult.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an appropriate HTTP result.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Converts a Result{T} to an appropriate HTTP result.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return ToErrorResult(result.Error);
    }

    /// <summary>
    /// Converts an Error to an appropriate HTTP result.
    /// </summary>
    public static IResult ToApiResult(this Error error) => ToErrorResult(error);

    private static IResult ToErrorResult(Error error)
    {
        var apiError = new ApiError(error.Code, error.Message);

        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(apiError),
            ErrorType.Validation => Results.BadRequest(apiError),
            ErrorType.Conflict => Results.Conflict(apiError),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.Json(apiError, statusCode: StatusCodes.Status403Forbidden),
            _ => Results.BadRequest(apiError)
        };
    }
}
