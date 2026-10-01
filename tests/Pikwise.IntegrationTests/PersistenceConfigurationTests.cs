using Microsoft.EntityFrameworkCore;
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
        Assert.Equal(4, context.Model.GetEntityTypes().Count());
        Assert.Single(context.Database.GetMigrations());
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
