using GeneFlow.ApiNet2.Application.Behaviors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace GeneFlow.ApiNet2.Infrastructure.Services;

/// <summary>
/// Validates worker API keys from the X-Worker-Api-Key header.
/// </summary>
public sealed class WorkerApiKeyValidator : IWorkerApiKeyValidator
{
    private const string HeaderName = "X-Worker-Api-Key";
    private readonly string? _validApiKey;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkerApiKeyValidator(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _validApiKey = configuration["Worker:ApiKey"];
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsValidApiKey
    {
        get
        {
            if (string.IsNullOrEmpty(_validApiKey))
            {
                // If no API key is configured, reject all requests
                return false;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return false;
            }

            if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue))
            {
                return false;
            }

            return string.Equals(_validApiKey, headerValue.ToString(), StringComparison.Ordinal);
        }
    }
}
