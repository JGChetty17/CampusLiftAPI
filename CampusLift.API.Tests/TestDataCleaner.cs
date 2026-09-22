using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace CampusLift.API.Tests;

public static class TestDataCleaner
{
    // Children before parents (FK-safe order)
    private static readonly string[] TablesInOrder =
    {
        "notifications",
        "messages",
        "ratings",
        "bookings",
        "trips",
        "vehicles",
        "users"
    };

    public static async Task CleanAllAsync(IConfiguration config)
    {
        var url = config["Supabase:Url"]!.TrimEnd('/');
        var key = config["Supabase:Key"]!;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("apikey", key);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        foreach (var table in TablesInOrder)
        {
            // "id is not null" matches every row
            var resp = await http.DeleteAsync($"{url}/rest/v1/{table}?id=not.is.null");
            resp.EnsureSuccessStatusCode();
        }
    }
}