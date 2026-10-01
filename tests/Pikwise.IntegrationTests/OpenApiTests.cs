using System.Text.Json;

namespace Pikwise.IntegrationTests;

public class OpenApiTests
{
    [Fact]
    public async Task Document_contains_get_product_by_id_contract()
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/products/{id}").GetProperty("get");

        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(operation.GetProperty("responses").TryGetProperty("404", out _));
    }
}
