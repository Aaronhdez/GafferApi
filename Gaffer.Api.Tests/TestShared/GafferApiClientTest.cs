using System.Net;
using System.Text;
using Gaffer.Api.Shared.Clients;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Gaffer.Api.Tests.TestShared;

public class GafferApiClientTest(HttpClient client) : IGafferApiClient
{
    public async Task<TResult?> PostAsync<TRequest, TResult>(Uri uri, TRequest body)
    {
            var formattedRequest = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            var result = await client.PostAsync(uri, new StringContent(formattedRequest, Encoding.UTF8, "application/json"));
            if (result.StatusCode == HttpStatusCode.OK) return await result.Content.ReadFromJsonAsync<TResult>();
            var content = await result.Content.ReadAsStringAsync();
            throw new GafferApiClientException(result.StatusCode, content);
    }
    
    public async Task<TResult?> GetAsync<TResult>(Uri uri)
    {
        var result = await client.GetAsync(uri);
        if (result.StatusCode == HttpStatusCode.OK) return await result.Content.ReadFromJsonAsync<TResult>();
        var content = await result.Content.ReadAsStringAsync();
        throw new GafferApiClientException(result.StatusCode, content);
    }

    public async Task UpdateAsync<TRequest>(Uri uri, TRequest body)
    {
        var formattedRequest = Newtonsoft.Json.JsonConvert.SerializeObject(body);
        var result = await client.PutAsync(uri, new StringContent(formattedRequest, Encoding.UTF8, "application/json"));
        if (result.StatusCode == HttpStatusCode.OK) return;
        var content = await result.Content.ReadAsStringAsync();
        throw new GafferApiClientException(result.StatusCode, content);
    }

    public async Task DeleteAsync(Uri uri)
    {
        var result = await client.DeleteAsync(uri);
        if (result.StatusCode == HttpStatusCode.OK) return;
        var content = await result.Content.ReadAsStringAsync();
        throw new GafferApiClientException(result.StatusCode, content);;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}

public class GafferApiClientException(HttpStatusCode resultStatusCode, string exMessage) : Exception($"{resultStatusCode}");