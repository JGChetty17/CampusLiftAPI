using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CampusLift.API.Tests;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IConfiguration Config;

    protected BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Config = factory.Services.GetRequiredService<IConfiguration>();
    }

    public async Task InitializeAsync()
    {
        await TestDataCleaner.CleanAllAsync(Config);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected void AsUser(string firebaseUid)
    {
        Client.DefaultRequestHeaders.Remove("X-Firebase-Uid");
        Client.DefaultRequestHeaders.Add("X-Firebase-Uid", firebaseUid);
    }

    protected async Task<T?> GetJsonAsync<T>(string url)
    {
        var resp = await Client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>();
    }

    protected Task<HttpResponseMessage> PostJsonAsync(string url, object body)
        => Client.PostAsJsonAsync(url, body);

    protected Task<HttpResponseMessage> PatchJsonAsync(string url, object body)
        => Client.PatchAsJsonAsync(url, body);
}