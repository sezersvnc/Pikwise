using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Infrastructure.Persistence;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Infrastructure.Products;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Infrastructure.Users;
using Pikwise.Application.Favorites.Interfaces;
using Pikwise.Infrastructure.Favorites;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Infrastructure.Recommendations;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Infrastructure.Llm;

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
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        // No language model provider is configured yet; parsing requests answer 503 (ADR-026).
        services.AddSingleton<IRequirementExtractor, UnconfiguredRequirementExtractor>();

        return services;
    }
}
