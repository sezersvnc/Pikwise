using System.Net.Http.Headers;
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
using Pikwise.Application.Explanations.Interfaces;
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
        services.AddLanguageModel(configuration);

        return services;
    }

    // Groq when Groq:ApiKey is configured (ADR-028); otherwise both language model endpoints answer 503.
    private static void AddLanguageModel(this IServiceCollection services, IConfiguration configuration)
    {
        var groq = GroqOptions.FromConfiguration(configuration);
        if (groq is null)
        {
            services.AddSingleton<IRequirementExtractor, UnconfiguredRequirementExtractor>();
            services.AddSingleton<IExplanationGenerator, UnconfiguredExplanationGenerator>();
            return;
        }

        services.AddSingleton(groq);
        services.AddHttpClient<GroqChatClient>(client =>
            {
                client.BaseAddress = groq.BaseUri;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", groq.ApiKey);
                // Safety net only; the Application services stop waiting after 15 seconds.
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .RedactLoggedHeaders(["Authorization"]);
        services.AddTransient<IRequirementExtractor, GroqRequirementExtractor>();
        services.AddTransient<IExplanationGenerator, GroqExplanationGenerator>();
    }
}
