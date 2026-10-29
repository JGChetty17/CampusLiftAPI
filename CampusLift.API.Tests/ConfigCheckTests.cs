using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace CampusLift.API.Tests;

[Collection("Sequential")]                       // ← ADD THIS LINE
public class ConfigCheckTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public ConfigCheckTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        : base(factory)
    {
        _output = output;
    }

    [Fact]
    public void Config_HasSupabaseUrlAndKey()
    {
        var url = Config["Supabase:Url"];
        var key = Config["Supabase:Key"];

        _output.WriteLine($"Supabase:Url length = {url?.Length ?? 0}");
        _output.WriteLine($"Supabase:Key length = {key?.Length ?? 0}");
        _output.WriteLine($"Url starts with https = {url?.StartsWith("https://")}");
        _output.WriteLine($"Env var direct = {Environment.GetEnvironmentVariable("Supabase__Url")?.Length}");

        Assert.False(string.IsNullOrWhiteSpace(url), "Supabase:Url is null or empty");
        Assert.False(string.IsNullOrWhiteSpace(key), "Supabase:Key is null or empty");
        Assert.True(url!.StartsWith("https://"), $"Url doesn't start with https:// — got '{url}'");
    }
}