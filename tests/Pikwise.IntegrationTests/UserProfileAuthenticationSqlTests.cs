using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Users.DTOs;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Infrastructure.Users;

namespace Pikwise.IntegrationTests;

public class UserProfileAuthenticationSqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Authenticated_profiles_persist_remain_isolated_and_handle_duplicate_creation()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        using var tokens = new AuthTestTokens();
        await using var baseFactory = new PikwiseApiFactory(connection);
        await using var factory = tokens.Configure(baseFactory);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create());
        try
        {
            // Concurrent first HTTP requests must resolve to the same SQL profile.
            var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me")));
            var first = responses[0]!;
            Assert.All(responses, profile => Assert.Equal(first, profile));
            Assert.Equal(1, await database.UserProfiles.CountAsync(u => u.AuthProviderUserId == tokens.Subject));

            // Exercise the unique-index recovery path even when the competing insert already finished.
            await using var competingContext = new ApplicationDbContext(options);
            var resolved = await new UserProfileRepository(competingContext).AddOrGetExistingAsync(new UserProfile
            { AuthProviderUserId = tokens.Subject, Email = "different@example.test", CreatedAt = DateTimeOffset.UtcNow });
            Assert.Equal(first.Id, resolved.Id);
            Assert.Equal(first.Email, resolved.Email);

            client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create(subject: tokens.OtherSubject));
            var second = (await client.GetFromJsonAsync<UserProfileResponseDto>("/api/auth/me"))!;
            Assert.NotEqual(first.Id, second.Id);
            Assert.Equal(tokens.OtherSubject, second.AuthProviderUserId);
            using var forbidden = await client.GetAsync("/api/auth/admin-check");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            var local = await database.UserProfiles.SingleAsync(u => u.Id == second.Id);
            local.Role = "Admin";
            await database.SaveChangesAsync();
            using var allowed = await client.GetAsync("/api/auth/admin-check");
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        }
        finally
        {
            // Remove only these temporary identities; the application database is never used by this test.
            await database.UserProfiles.Where(u => u.AuthProviderUserId == tokens.Subject || u.AuthProviderUserId == tokens.OtherSubject)
                .ExecuteDeleteAsync();
        }
    }
}
