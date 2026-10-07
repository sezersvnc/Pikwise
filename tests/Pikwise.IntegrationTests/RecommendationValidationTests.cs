using System.Net;
using System.Text;
using System.Text.Json;

namespace Pikwise.IntegrationTests;

public class RecommendationValidationTests
{
    [Theory]
    [InlineData("""{"budgetMax":0}""")]
    [InlineData("""{"budgetMax":-5}""")]
    [InlineData("""{"budgetMax":10000000.01}""")]
    [InlineData("""{"budgetMax":100.001}""")]
    [InlineData("""{"minRamGb":0}""")]
    [InlineData("""{"minRamGb":257}""")]
    [InlineData("""{"minStorageGb":0}""")]
    [InlineData("""{"maxWeightKg":0.05}""")]
    [InlineData("""{"maxWeightKg":1.555}""")]
    [InlineData("""{"importance":{"ram":0}}""")]
    [InlineData("""{"importance":{"weight":6}}""")]
    [InlineData("""{"budgetMax":"cheap"}""")]
    [InlineData("""{"minRamGb":2147483648}""")]
    [InlineData("""{"budgetMax":""")]
    public async Task Invalid_request_returns_400_without_database_access(string body)
    {
        await using var factory = new PikwiseApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/recommendations", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(json.RootElement.GetProperty("errors").EnumerateObject());
    }
}
