using Pikwise.Infrastructure;
using Pikwise.Application.Products.Interfaces;
using Pikwise.Application.Products.Services;
using Pikwise.Api.Errors;
using Pikwise.Api.Authentication;
using Pikwise.Application.Favorites.Interfaces;
using Pikwise.Application.Favorites.Services;
using Pikwise.Application.Recommendations.Interfaces;
using Pikwise.Application.Recommendations.Services;
using Pikwise.Application.RequirementParsing.Interfaces;
using Pikwise.Application.RequirementParsing.Services;

var builder = WebApplication.CreateBuilder(args);
// Compose dependencies at startup; controllers receive their services through DI.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSupabaseAuthentication(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IRequirementParsingService, RequirementParsingService>();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var app = builder.Build();
// Register before endpoints so exceptions from the request pipeline reach the central handler.
app.UseExceptionHandler();
// Establish identity before evaluating endpoint authorization policies.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness only: this endpoint does not check database availability.
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Expose the entry point to WebApplicationFactory integration tests.
public partial class Program;
