using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pikwise.IntegrationTests;

public sealed class PikwiseApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configuration-only tests: no SQL Server connection is opened.
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Server=localhost;Database=PikwiseConfigurationTests;Integrated Security=True;Encrypt=True;");
    }
}
