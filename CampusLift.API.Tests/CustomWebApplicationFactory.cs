using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CampusLift.API.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Point the content root at the test project's output folder
        // so appsettings.Testing.json resolves correctly.
        var testProjectDir = AppContext.BaseDirectory;
        builder.UseContentRoot(testProjectDir);

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddJsonFile(
                Path.Combine(testProjectDir, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);
            config.AddEnvironmentVariables();
        });
    }
}