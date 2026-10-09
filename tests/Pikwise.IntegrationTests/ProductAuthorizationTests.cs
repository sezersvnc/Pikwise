using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pikwise.Application.Products.DTOs;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Users.DTOs;
using Pikwise.Application.Users.Interfaces;

namespace Pikwise.IntegrationTests;

// Product writes require the LocalAdmin policy (ADR-029). The profile and product services are
// fakes, so these tests need no SQL Server; ProductCrudTests covers the real Admin path.
public class ProductAuthorizationTests
{
    public static TheoryData<string, string> Writes => new()
    {
        { "POST", "/api/products" },
        { "PUT", "/api/products/1" },
        { "DELETE", "/api/products/1" }
    };

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task Anonymous_write_returns_401(string method, string path)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = Configure(tokens, baseFactory, role: "Admin");
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Request(method, path, token: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Writes))]
    public async Task Write_by_a_regular_user_returns_403(string method, string path)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = Configure(tokens, baseFactory, role: "User");
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Request(method, path, tokens.Create()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_passes_authorization()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = Configure(tokens, baseFactory, role: "Admin");
        using var client = factory.CreateClient();

        // The fake service reports the product as missing, so 404 proves the request was authorized.
        using var response = await client.SendAsync(Request("DELETE", "/api/products/1", tokens.Create()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reads_stay_public()
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = Configure(tokens, baseFactory, role: "User");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Configure(
        AuthTestTokens tokens, PikwiseApiFactory factory, string role) =>
        tokens.Configure(factory, services =>
        {
            services.RemoveAll<IUserProfileService>();
            services.AddScoped<IUserProfileService>(_ => new FakeProfiles(role));
            services.RemoveAll<IProductService>();
            services.AddScoped<IProductService, MissingProducts>();
        });

    private static HttpRequestMessage Request(string method, string path, string? token)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method != "DELETE") request.Content = JsonContent.Create(new { name = "Laptop" });
        if (token is not null) request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private sealed class FakeProfiles(string role) : IUserProfileService
    {
        public Task<UserProfileResponseDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileResponseDto?>(new(1, "subject", "user@example.test", role, DateTimeOffset.UtcNow));
    }

    // Every product is missing; write methods are only reached after authorization.
    private sealed class MissingProducts : IProductService
    {
        public Task<ProductResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductResponseDto?>(null);
        public Task<ProductComparisonResponseDto> CompareAsync(ProductComparisonRequestDto request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<PagedProductResponseDto> GetAllAsync(ProductQueryRequestDto query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ProductResponseDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<ProductResponseDto?> UpdateAsync(int id, UpdateProductRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductResponseDto?>(null);
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
