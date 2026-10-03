using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Pikwise.Application.Favorites.DTOs;
using Pikwise.Application.Products.Exceptions;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.Favorites;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class FavoriteApiSqlTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Favorites_persist_only_for_the_caller_handle_duplicates_and_preserve_parents()
    {
        var connection = Environment.GetEnvironmentVariable("PIKWISE_TEST_CONNECTION")!;
        Assert.Equal("PikwiseSession3Tests", new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        await using var database = new ApplicationDbContext(options);
        await database.Database.MigrateAsync();
        using var tokens = new AuthTestTokens();
        var noEmailSubject = Guid.NewGuid().ToString();
        var suffix = Guid.NewGuid().ToString("N");
        var brand = new Brand { Name = "Favorite API brand " + suffix };
        var category = new Category { Name = "Favorite API category " + suffix };
        var first = new Product
        {
            Name = "Favorite API first", Brand = brand, Category = category, Price = 30000m,
            Stock = 1, IsActive = true, CreatedAt = DateTimeOffset.UtcNow,
            LaptopSpecification = new LaptopSpecification
            {
                Processor = "CPU", GPU = "GPU", RamGb = 16, StorageGb = 512, ScreenSize = 15.6m,
                Resolution = "1920x1080", RefreshRate = 60, Weight = 1.8m, OperatingSystem = "OS"
            }
        };
        // The current favorite contract accepts any existing product, including inactive ones.
        var second = new Product { Name = "Favorite API second", Brand = brand, Category = category, Price = 20000m,
            Stock = 0, IsActive = false, CreatedAt = DateTimeOffset.UtcNow };
        database.AddRange(first, second);
        await database.SaveChangesAsync();
        await using var baseFactory = new PikwiseApiFactory(connection);
        await using var factory = tokens.Configure(baseFactory);
        using var caller = factory.CreateClient();
        using var other = factory.CreateClient();
        caller.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create());
        other.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create(subject: tokens.OtherSubject));
        try
        {
            Assert.Empty((await caller.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!);
            using var added = await caller.PostAsJsonAsync($"/api/favorites/{first.Id}?userProfileId=2147483647", new { userProfileId = int.MaxValue });
            Assert.Equal(HttpStatusCode.Created, added.StatusCode);
            var saved = (await added.Content.ReadFromJsonAsync<FavoriteResponseDto>())!;
            Assert.Equal(first.Id, saved.Product.Id);
            Assert.Equal(brand.Name, saved.Product.Brand.Name);
            Assert.Equal("CPU", saved.Product.Specification!.Processor);
            Assert.Equal(TimeSpan.Zero, saved.CreatedAt.Offset);
            var body = await added.Content.ReadAsStringAsync();
            Assert.DoesNotContain("userProfileId", body);
            Assert.DoesNotContain("email", body);
            using var duplicate = await caller.PostAsync($"/api/favorites/{first.Id}", null);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType!.MediaType);
            Assert.Equal(saved.CreatedAt, (await caller.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!.Single().CreatedAt);

            Assert.Empty((await other.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!);
            using var notOwned = await other.DeleteAsync($"/api/favorites/{first.Id}");
            Assert.Equal(HttpStatusCode.NotFound, notOwned.StatusCode);
            using var otherAdded = await other.PostAsync($"/api/favorites/{first.Id}", null);
            Assert.Equal(HttpStatusCode.Created, otherAdded.StatusCode);
            var otherId = (await database.UserProfiles.SingleAsync(u => u.AuthProviderUserId == tokens.OtherSubject)).Id;
            Assert.Single((await caller.GetFromJsonAsync<List<FavoriteResponseDto>>($"/api/favorites?userProfileId={otherId}"))!);

            // One insert wins; all concurrent duplicates return a controlled 409.
            var concurrent = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => caller.PostAsync($"/api/favorites/{second.Id}", null)));
            try
            {
                Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.Created);
                Assert.Equal(3, concurrent.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            }
            finally { foreach (var response in concurrent) response.Dispose(); }
            var list = (await caller.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!;
            Assert.Equal(2, list.Count);
            Assert.Equal(second.Id, list[0].Product.Id);
            Assert.Null(list[0].Product.Specification);
            Assert.Single((await other.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!);
            using var removed = await caller.DeleteAsync($"/api/favorites/{first.Id}");
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.True(await database.Favorites.AnyAsync(f => f.UserProfileId == otherId && f.ProductId == first.Id));
            Assert.True(await database.Products.AnyAsync(p => p.Id == first.Id));
            Assert.True(await database.UserProfiles.AnyAsync(u => u.Id == otherId));
            using var alreadyRemoved = await caller.DeleteAsync($"/api/favorites/{first.Id}");
            Assert.Equal(HttpStatusCode.NotFound, alreadyRemoved.StatusCode);
            using var missingProduct = await caller.PostAsync("/api/favorites/2147483647", null);
            Assert.Equal(HttpStatusCode.NotFound, missingProduct.StatusCode);
            using var missingDelete = await caller.DeleteAsync("/api/favorites/2147483647");
            Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
            foreach (var id in new[] { 0, -1 })
            {
                using var invalidPost = await caller.PostAsync($"/api/favorites/{id}", null);
                using var invalidDelete = await caller.DeleteAsync($"/api/favorites/{id}");
                Assert.Equal(HttpStatusCode.BadRequest, invalidPost.StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, invalidDelete.StatusCode);
            }
            caller.DefaultRequestHeaders.Authorization = new("Bearer", tokens.Create("no-email", noEmailSubject));
            using var forbidden = await caller.GetAsync("/api/favorites");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            Assert.False(await database.UserProfiles.AnyAsync(u => u.AuthProviderUserId == noEmailSubject));

            await using var conflictContext = new ApplicationDbContext(options);
            await Assert.ThrowsAsync<PersistenceConflictException>(() => new FavoriteRepository(conflictContext).AddAsync(
                new Favorite { UserProfileId = otherId, ProductId = int.MaxValue, CreatedAt = DateTimeOffset.UtcNow }));
            await database.Products.Where(p => p.Id == first.Id).ExecuteDeleteAsync();
            Assert.Empty((await other.GetFromJsonAsync<List<FavoriteResponseDto>>("/api/favorites"))!);
        }
        finally
        {
            // Delete only this test's identities and products; database cascades remove their favorites.
            await database.UserProfiles.Where(u => u.AuthProviderUserId == tokens.Subject || u.AuthProviderUserId == tokens.OtherSubject)
                .ExecuteDeleteAsync();
            await database.Products.Where(p => p.BrandId == brand.Id).ExecuteDeleteAsync();
            await database.Brands.Where(b => b.Id == brand.Id).ExecuteDeleteAsync();
            await database.Categories.Where(c => c.Id == category.Id).ExecuteDeleteAsync();
        }
    }
}
