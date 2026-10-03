using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pikwise.IntegrationTests;

public sealed class PikwiseApiFactory(string? connectionString = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configuration-only tests: no SQL Server connection is opened.
        builder.UseSetting("Authentication:Supabase:Issuer", "https://auth.example.test/auth/v1");
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            connectionString ??
            "Server=localhost;Database=PikwiseConfigurationTests;Integrated Security=True;Encrypt=True;");
    }
}
