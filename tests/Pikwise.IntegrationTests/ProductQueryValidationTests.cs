using System.Net;
using System.Text.Json;

namespace Pikwise.IntegrationTests;

public class ProductQueryValidationTests
{
    [Theory]
    [InlineData("minPrice=-1")]
    [InlineData("maxPrice=1.001")]
    [InlineData("minPrice=500&maxPrice=100")]
    [InlineData("brandId=0")]
    [InlineData("minRam=0")]
    [InlineData("minStorage=-1")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("sortBy=stock")]
    [InlineData("sortDirection=sideways")]
    [InlineData("minPrice=not-a-number")]
    public async Task Invalid_queries_return_validation_problem_without_database_access(string query)
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/products?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(document.RootElement.GetProperty("errors").EnumerateObject());
    }
}
