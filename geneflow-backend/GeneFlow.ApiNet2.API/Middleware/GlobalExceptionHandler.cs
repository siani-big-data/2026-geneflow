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
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
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

        var message = _environment.IsDevelopment()
            ? $"{exception.Message} | {exception.InnerException?.Message}"
            : "An unexpected error occurred. Please try again later.";

        var error = new ApiError(
            "InternalServerError",
            message);

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);

        return true;
    }
}
