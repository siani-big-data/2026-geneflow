using System.Net.Http.Headers;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.Tests.API;

namespace GeneFlow.ApiNet2.Tests.Common.Base;

/// <summary>
/// Base class for API endpoint integration tests.
/// </summary>
public abstract class EndpointTestBase : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    protected readonly GeneFlowWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected EndpointTestBase(GeneFlowWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Sets the authorization header with a JWT token.
    /// </summary>
    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Posts JSON content and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T content)
    {
        return await Client.PostAsJsonAsync(url, content);
    }

    /// <summary>
    /// Puts JSON content and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T content)
    {
        return await Client.PutAsJsonAsync(url, content);
    }

    /// <summary>
    /// Gets and deserializes a response.
    /// </summary>
    protected async Task<T?> GetAsync<T>(string url)
    {
        return await Client.GetFromJsonAsync<T>(url);
    }

    /// <summary>
    /// Deletes a resource and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.ResetMocks();
        GC.SuppressFinalize(this);
    }
}
