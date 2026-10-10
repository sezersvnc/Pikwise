using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Infrastructure;
using Pikwise.Infrastructure.Persistence;

namespace Pikwise.IntegrationTests;

public class PersistenceConfigurationTests
{
    [Fact]
    public async Task Host_registers_sql_server_context_with_scoped_lifetime()
    {
        await using var factory = new PikwiseApiFactory();
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var context = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Same(context, firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        Assert.NotSame(context, secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        // Session 11.5 adds ProductExternalReference and its migration.
        Assert.Equal(7, context.Model.GetEntityTypes().Count());
        // ADR-030 adds the explicit text collation migration.
        Assert.Equal(4, context.Database.GetMigrations().Count());
    }

    [Fact]
    public async Task Every_text_column_states_its_collation()
    {
        // A text column without a collation would follow the server default again (ADR-030).
        await using var factory = new PikwiseApiFactory();
        using var scope = factory.Services.CreateScope();
        // Collation is schema metadata: EF keeps it in the design-time model that migrations use.
        var model = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .GetService<IDesignTimeModel>().Model;
        var text = model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(string)).ToList();

        Assert.NotEmpty(text);
        Assert.All(text, property => Assert.NotNull(property.GetCollation()));
        Assert.Equal(new[] { "AuthProviderUserId", "ExternalId" }, text
            .Where(property => property.GetCollation() != ApplicationDbContext.DefaultTextCollation)
            .Select(property => property.Name).Order());
        Assert.All(text.Where(property => property.GetCollation() != ApplicationDbContext.DefaultTextCollation),
            property => Assert.Equal("Latin1_General_100_BIN2", property.GetCollation()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_connection_is_rejected_with_configuration_guidance(string? connectionString)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            }).Build();

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:DefaultConnection", error.Message);
    }
}
