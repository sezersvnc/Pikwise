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
        var paths = document.RootElement.GetProperty("paths");
        var me = paths.GetProperty("/api/auth/me").GetProperty("get").GetProperty("responses");
        Assert.True(me.TryGetProperty("200", out _));
        Assert.True(me.TryGetProperty("401", out _));
        Assert.True(me.TryGetProperty("403", out _));
        Assert.True(paths.TryGetProperty("/api/auth/admin-check", out _));
        Assert.True(paths.GetProperty("/api/favorites").GetProperty("get").GetProperty("responses").TryGetProperty("200", out _));
        var favoriteWrites = paths.GetProperty("/api/favorites/{productId}");
        Assert.True(favoriteWrites.GetProperty("post").GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(favoriteWrites.GetProperty("post").GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(favoriteWrites.GetProperty("delete").GetProperty("responses").TryGetProperty("204", out _));
        Assert.True(paths.GetProperty("/api/products").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/products").GetProperty("post").GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(paths.GetProperty("/api/products/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/products/{id}").GetProperty("delete").GetProperty("responses").TryGetProperty("204", out _));
    }
}
