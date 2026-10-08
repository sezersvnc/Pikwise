using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pikwise.Application.Explanations.Exceptions;
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Explanations.Models;
using Pikwise.Application.RequirementParsing.Exceptions;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Application.RequirementParsing.Models;
using Pikwise.Infrastructure;
using Pikwise.Infrastructure.Llm;

namespace Pikwise.UnitTests;

// The real registration from AddInfrastructure is used; only the network handler is replaced.
public class GroqProviderTests
{
    private const string Key = "test-groq-key-never-logged";
    private const string UserText = "50 bin TL bütçem var, gizli kullanıcı metni";

    [Fact]
    public async Task Requirement_request_uses_strict_json_schema_and_bearer_key_and_returns_message_content()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, Completion("""{"minRamGb":16}"""));
        await using var provider = Build(handler);

        var result = await provider.GetRequiredService<IRequirementExtractor>().ExtractAsync(UserText, CancellationToken.None);

        Assert.Equal("""{"minRamGb":16}""", result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", request.Uri);
        Assert.Equal("Bearer " + Key, request.Authorization);
        var body = JsonNode.Parse(request.Body)!;
        Assert.Equal("openai/gpt-oss-120b", (string)body["model"]!);
        Assert.Equal("system", (string)body["messages"]![0]!["role"]!);
        Assert.Equal(RequirementExtractionContract.Instructions, (string)body["messages"]![0]!["content"]!);
        Assert.Equal("user", (string)body["messages"]![1]!["role"]!);
        Assert.Equal(UserText, (string)body["messages"]![1]!["content"]!);
        Assert.Equal(2, body["messages"]!.AsArray().Count);
        var format = body["response_format"]!;
        Assert.Equal("json_schema", (string)format["type"]!);
        Assert.True((bool)format["json_schema"]!["strict"]!);
        Assert.Equal("recommendation_criteria", (string)format["json_schema"]!["name"]!);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(RequirementExtractionContract.JsonSchema), format["json_schema"]!["schema"]));
        Assert.Equal("low", (string)body["reasoning_effort"]!);
        Assert.False((bool)body["include_reasoning"]!);
        Assert.Equal(2048, (int)body["max_completion_tokens"]!);
    }

    [Fact]
    public async Task Explanation_request_sends_the_input_json_with_the_explanation_contract()
    {
        var handler = new FakeHandler(HttpStatusCode.OK, Completion("""{"products":[],"valueComment":null}"""));
        await using var provider = Build(handler);

        await provider.GetRequiredService<IExplanationGenerator>().GenerateAsync("""{"products":[]}""", CancellationToken.None);

        var body = JsonNode.Parse(Assert.Single(handler.Requests).Body)!;
        Assert.Equal(ExplanationContract.Instructions, (string)body["messages"]![0]!["content"]!);
        Assert.Equal("""{"products":[]}""", (string)body["messages"]![1]!["content"]!);
        Assert.Equal("recommendation_explanation", (string)body["response_format"]!["json_schema"]!["name"]!);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(ExplanationContract.JsonSchema), body["response_format"]!["json_schema"]!["schema"]));
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Error_status_becomes_unavailable_and_logs_only_the_status(HttpStatusCode status)
    {
        var logs = new List<string>();
        await using var provider = Build(new FakeHandler(status, """{"error":{"message":"echo: 50 bin TL"}}"""), logs);

        await Assert.ThrowsAsync<RequirementExtractionUnavailableException>(
            () => provider.GetRequiredService<IRequirementExtractor>().ExtractAsync(UserText, CancellationToken.None));
        await Assert.ThrowsAsync<ExplanationUnavailableException>(
            () => provider.GetRequiredService<IExplanationGenerator>().GenerateAsync("{}", CancellationToken.None));

        Assert.Contains(logs, line => line.Contains(((int)status).ToString()));
        AssertNoSecretsOrText(logs);
    }

    [Fact]
    public async Task Network_failure_becomes_unavailable()
    {
        await using var provider = Build(new FakeHandler(new HttpRequestException("connection refused")));

        await Assert.ThrowsAsync<RequirementExtractionUnavailableException>(
            () => provider.GetRequiredService<IRequirementExtractor>().ExtractAsync(UserText, CancellationToken.None));
    }

    [Fact]
    public async Task Unreadable_response_becomes_unavailable()
    {
        await using var provider = Build(new FakeHandler(HttpStatusCode.OK, "<html>gateway</html>"));

        await Assert.ThrowsAsync<ExplanationUnavailableException>(
            () => provider.GetRequiredService<IExplanationGenerator>().GenerateAsync("{}", CancellationToken.None));
    }

    [Fact]
    public async Task Missing_content_returns_empty_output_for_the_service_to_reject()
    {
        await using var provider = Build(new FakeHandler(HttpStatusCode.OK, """{"choices":[]}"""));

        var result = await provider.GetRequiredService<IRequirementExtractor>().ExtractAsync(UserText, CancellationToken.None);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task Successful_call_logs_neither_the_key_nor_the_user_text_even_at_trace_level()
    {
        var logs = new List<string>();
        await using var provider = Build(new FakeHandler(HttpStatusCode.OK, Completion("{}")), logs);

        await provider.GetRequiredService<IRequirementExtractor>().ExtractAsync(UserText, CancellationToken.None);

        Assert.NotEmpty(logs);
        AssertNoSecretsOrText(logs);
    }

    [Fact]
    public void Options_come_from_configuration_and_a_blank_key_means_unconfigured()
    {
        var options = GroqOptions.FromConfiguration(Configuration(new()
        {
            ["Groq:ApiKey"] = " key ", ["Groq:Model"] = "openai/gpt-oss-20b",
            ["Groq:ReasoningEffort"] = "medium", ["Groq:MaxCompletionTokens"] = "512"
        }))!;

        Assert.Equal("key", options.ApiKey);
        Assert.Equal("openai/gpt-oss-20b", options.Model);
        Assert.Equal("medium", options.ReasoningEffort);
        Assert.Equal(512, options.MaxCompletionTokens);
        Assert.DoesNotContain("key", options.ToString());
        Assert.Null(GroqOptions.FromConfiguration(Configuration(new() { ["Groq:ApiKey"] = "  " })));
        Assert.Null(GroqOptions.FromConfiguration(Configuration(new())));
    }

    private static void AssertNoSecretsOrText(IEnumerable<string> logs)
    {
        foreach (var line in logs)
        {
            Assert.DoesNotContain(Key, line);
            Assert.DoesNotContain("50 bin TL", line);
        }
    }

    private static string Completion(string content) =>
        JsonSerializer.Serialize(new { choices = new[] { new { message = new { role = "assistant", content } } } });

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider Build(FakeHandler handler, List<string>? logs = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(new ListLoggerProvider(logs ?? [])));
        // AddDbContext does not connect; the connection string only satisfies startup validation.
        services.AddInfrastructure(Configuration(new()
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Unused;Integrated Security=True;",
            ["Groq:ApiKey"] = Key
        }));
        services.AddHttpClient<GroqChatClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private sealed record CapturedRequest(HttpMethod Method, string Uri, string? Authorization, string Body);

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode status;
        private readonly string responseBody;
        private readonly Exception? failure;

        public FakeHandler(HttpStatusCode status, string responseBody) => (this.status, this.responseBody) = (status, responseBody);
        public FakeHandler(Exception failure) : this(HttpStatusCode.OK, string.Empty) => this.failure = failure;

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!.ToString(),
                request.Headers.Authorization?.ToString(), await request.Content!.ReadAsStringAsync(cancellationToken)));
            if (failure is not null) throw failure;
            return new HttpResponseMessage(status) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class ListLoggerProvider(List<string> lines) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(lines);
        public void Dispose() { }

        private sealed class ListLogger(List<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (lines) lines.Add(formatter(state, exception) + " " + exception);
            }
        }
    }
}
