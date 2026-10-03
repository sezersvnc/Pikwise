using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pikwise.Application.Users.DTOs;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Domain.Entities;

namespace Pikwise.IntegrationTests;

public class AuthenticationTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("unsigned")]
    [InlineData("hs256")]
    [InlineData("no-exp")]
    [InlineData("no-sub")]
    [InlineData("blank-sub")]
    [InlineData("long-sub")]
    [InlineData("duplicate-sub")]
    [InlineData("service-role")]
    public async Task Invalid_identity_returns_401_before_profile_access(string variant)
    {
        using var tokens = new AuthTestTokens();
        var repository = new MemoryProfiles();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory, services => ReplaceRepository(services, repository));
        using var client = factory.CreateClient();
        if (variant != "missing")
            client.DefaultRequestHeaders.Authorization = new("Bearer", variant == "malformed" ? "invalid" : tokens.Create(variant));
        using var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, h => h.Scheme == "Bearer");
        Assert.Equal(0, repository.Reads);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("rsa")]
    public async Task Valid_token_provisions_own_profile_and_reuses_it(string variant)
    {
        using var tokens = new AuthTestTokens();
        var repository = new MemoryProfiles();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory, services => ReplaceRepository(services, repository));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create(variant));
        var first = await client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me?userProfileId=999");
        var second = await client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me");
        Assert.NotNull(first);
        Assert.Equal(tokens.Subject, first.AuthProviderUserId);
        Assert.Equal("User", first.Role);
        Assert.Equal(TimeSpan.Zero, first.CreatedAt.Offset);
        Assert.Equal(first, second);
        Assert.Single(repository.Profiles);

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create(subject: tokens.OtherSubject));
        var other = await client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me");
        Assert.NotEqual(first.Id, other!.Id);
        Assert.Equal(tokens.OtherSubject, other.AuthProviderUserId);
    }

    [Fact]
    public async Task Local_role_controls_403_and_admin_success_without_trusting_metadata()
    {
        using var tokens = new AuthTestTokens();
        var repository = new MemoryProfiles();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory, services => ReplaceRepository(services, repository));
        using var client = factory.CreateClient();
        using var missing = await client.GetAsync("/api/auth/admin-check");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create("metadata-admin"));
        using var forbidden = await client.GetAsync("/api/auth/admin-check");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        repository.Profiles.Single().Role = "Admin";
        using var allowed = await client.GetAsync("/api/auth/admin-check");
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    [Theory]
    [InlineData("no-email")]
    [InlineData("invalid-email")]
    public async Task New_profile_requires_valid_email_but_existing_profile_does_not(string variant)
    {
        using var tokens = new AuthTestTokens();
        var repository = new MemoryProfiles();
        await using var baseFactory = new PikwiseApiFactory();
        await using var factory = tokens.Configure(baseFactory, services => ReplaceRepository(services, repository));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create(variant));
        using var denied = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Empty(repository.Profiles);
        repository.Profiles.Add(new UserProfile { Id = 42, AuthProviderUserId = tokens.Subject, Email = "existing@example.test", Role = "Admin" });
        var existing = await client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me");
        Assert.Equal(42, existing!.Id);
        Assert.Equal("Admin", existing.Role);
        Assert.Equal("existing@example.test", existing.Email);
    }

    private static void ReplaceRepository(IServiceCollection services, MemoryProfiles repository)
    {
        services.RemoveAll<IUserProfileRepository>();
        services.AddSingleton<IUserProfileRepository>(repository);
    }

    private sealed class MemoryProfiles : IUserProfileRepository
    {
        public List<UserProfile> Profiles { get; } = [];
        public int Reads { get; private set; }
        public Task<UserProfile?> GetBySubjectAsync(string subject, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(Profiles.SingleOrDefault(u => u.AuthProviderUserId == subject));
        }
        public Task<UserProfile> AddOrGetExistingAsync(UserProfile profile, CancellationToken cancellationToken = default)
        {
            profile.Id = Profiles.Count + 1;
            Profiles.Add(profile);
            return Task.FromResult(profile);
        }
    }
}
