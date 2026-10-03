using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pikwise.Application.Users.DTOs;
using Pikwise.Application.Users.Interfaces;

namespace Pikwise.IntegrationTests;

public class FavoriteEndpointTests
{
    [Theory]
    [InlineData("GET", "/api/favorites", "missing")]
    [InlineData("POST", "/api/favorites/1", "missing")]
    [InlineData("DELETE", "/api/favorites/1", "missing")]
    [InlineData("GET", "/api/favorites", "expired")]
    [InlineData("POST", "/api/favorites/1", "expired")]
    [InlineData("DELETE", "/api/favorites/1", "expired")]
    [InlineData("GET", "/api/favorites", "signature")]
    [InlineData("POST", "/api/favorites/1", "signature")]
    [InlineData("DELETE", "/api/favorites/1", "signature")]
    public async Task Every_favorite_operation_requires_a_valid_user_token(string method, string path, string token)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (token != "missing") request.Headers.Authorization = new("Bearer", tokens.Create(token));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/favorites")]
    [InlineData("POST", "/api/favorites/1")]
    [InlineData("DELETE", "/api/favorites/1")]
    public async Task Unresolved_profile_returns_403_before_favorite_database_access(string method, string path)
    {
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory, services =>
        {
            services.RemoveAll<IUserProfileService>();
            services.AddScoped<IUserProfileService, MissingProfile>();
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = new("Bearer", tokens.Create());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    private sealed class MissingProfile : IUserProfileService
    {
        public Task<UserProfileResponseDto?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileResponseDto?>(null);
    }
}
