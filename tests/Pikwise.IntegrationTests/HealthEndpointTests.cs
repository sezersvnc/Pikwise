using System.Net;

namespace Pikwise.IntegrationTests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_returns_ok_when_application_starts()
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
