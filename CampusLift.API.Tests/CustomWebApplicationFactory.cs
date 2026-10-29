using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;              // ← AddJsonFile
using Microsoft.Extensions.Configuration.EnvironmentVariables; // ← AddEnvironmentVariables
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var testProjectDir = AppContext.BaseDirectory;
        builder.UseContentRoot(testProjectDir);

        // Clear existing config and rebuild in a known order
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.Sources.Clear();                                    // ← NEW: start clean

            config.AddJsonFile(
                Path.Combine(testProjectDir, "appsettings.Testing.json"),
                optional: true,                                        // ← CHANGED: true (so missing file doesn't crash)
                reloadOnChange: false);

            config.AddEnvironmentVariables();                          // ← LAST = wins
        });
    }
}