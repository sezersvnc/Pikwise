using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Api.Authentication;

namespace Pikwise.IntegrationTests;

public class AuthenticationConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://auth.example.test/auth/v1")]
    [InlineData("https://auth.example.test")]
    [InlineData("https://auth.example.test/auth/v1?secret=value")]
    public void Invalid_issuer_is_rejected_without_exposing_configuration_values(string? issuer)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Authentication:Supabase:Issuer"] = issuer }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddSupabaseAuthentication(configuration));
        Assert.Contains("Authentication:Supabase:Issuer", error.Message);
        Assert.DoesNotContain("secret=value", error.Message);
    }
}
