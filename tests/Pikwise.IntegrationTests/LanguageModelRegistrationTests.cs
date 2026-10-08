using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Infrastructure.Llm;

namespace Pikwise.IntegrationTests;

public class LanguageModelRegistrationTests
{
    [Fact]
    public async Task Without_a_key_the_unconfigured_implementations_are_used_even_if_user_secrets_hold_one()
    {
        // PikwiseApiFactory clears Groq:ApiKey, so a developer's real key never reaches tests.
        await using var factory = new PikwiseApiFactory();
        using var scope = factory.Services.CreateScope();

        Assert.IsType<UnconfiguredRequirementExtractor>(scope.ServiceProvider.GetRequiredService<IRequirementExtractor>());
        Assert.IsType<UnconfiguredExplanationGenerator>(scope.ServiceProvider.GetRequiredService<IExplanationGenerator>());
    }

    [Fact]
    public async Task With_a_key_the_Groq_implementations_are_used()
    {
        await using var factory = new PikwiseApiFactory(settings: new Dictionary<string, string> { ["Groq:ApiKey"] = "test-key" });
        using var scope = factory.Services.CreateScope();

        Assert.IsType<GroqRequirementExtractor>(scope.ServiceProvider.GetRequiredService<IRequirementExtractor>());
        Assert.IsType<GroqExplanationGenerator>(scope.ServiceProvider.GetRequiredService<IExplanationGenerator>());
    }
}
