using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Infrastructure.Products;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Infrastructure.Users;

namespace Pikwise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Resolve external configuration and fail early without logging connection credentials.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:DefaultConnection using User Secrets or environment variables.");
        }

        // Scoped lifetimes give each request its own context, shared by its repositories.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        return services;
    }
}
