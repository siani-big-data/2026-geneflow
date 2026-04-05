using System.Net;
using GeneFlow.ApiNet2.API.Extensions;
using Microsoft.AspNetCore.Diagnostics;

namespace GeneFlow.ApiNet2.API.Middleware;

/// <summary>
/// Global exception handler for unhandled exceptions.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred: {Message}",
            exception.Message);

        var error = new ApiError(
            "InternalServerError",
            "An unexpected error occurred. Please try again later.");

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);

        return true;
    }
}
