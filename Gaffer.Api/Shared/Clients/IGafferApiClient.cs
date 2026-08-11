namespace Gaffer.Api.Shared.Clients;

public interface IGafferApiClient
{
    Task<TResult?> PostAsync<TRequest, TResult>(Uri uri, TRequest body);
    Task<TResult?> GetAsync<TResult>(Uri uri);
    void Dispose();
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}