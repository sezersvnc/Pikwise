using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pikwise.IntegrationTests;

public sealed class PikwiseApiFactory(
    string? connectionString = null,
    IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configuration-only tests: no SQL Server connection is opened.
        builder.UseSetting("Authentication:Supabase:Issuer", "https://auth.example.test/auth/v1");
        // Override a developer's User Secrets key so tests never call the real language model.
        builder.UseSetting("Groq:ApiKey", string.Empty);
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            connectionString ??
            "Server=localhost;Database=PikwiseConfigurationTests;Integrated Security=True;Encrypt=True;");
        // Test-specific settings are applied last so they win.
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
            builder.UseSetting(key, value);
    }
}
