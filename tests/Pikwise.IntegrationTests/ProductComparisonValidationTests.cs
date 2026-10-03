using System.Net;
using System.Text.Json;

namespace Pikwise.IntegrationTests;

public class ProductComparisonValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ids=1")]
    [InlineData("ids=1&ids=2&ids=3&ids=4")]
    [InlineData("ids=1&ids=1")]
    [InlineData("ids=1&ids=2&ids=1")]
    [InlineData("ids=0&ids=2")]
    [InlineData("ids=-1&ids=2")]
    [InlineData("ids=abc&ids=2")]
    [InlineData("ids=2147483648&ids=2")]
    public async Task Invalid_selection_returns_400_without_database_access(string query)
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/products/compare?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(json.RootElement.GetProperty("errors").EnumerateObject());
    }
}
