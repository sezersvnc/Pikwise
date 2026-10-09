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
using Pikwise.Application.Explanations.Interfaces;
using Pikwise.Application.Explanations.Services;
using Pikwise.Api.RateLimiting;
using Pikwise.Api.Cors;

var builder = WebApplication.CreateBuilder(args);
// Compose dependencies at startup; controllers receive their services through DI.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSupabaseAuthentication(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IRequirementParsingService, RequirementParsingService>();
builder.Services.AddScoped<IExplanationService, ExplanationService>();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddLanguageModelRateLimiting(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);
var app = builder.Build();
// Register before endpoints so exceptions from the request pipeline reach the central handler.
app.UseExceptionHandler();
// CORS before authentication, so 401, 403 and 429 responses also carry CORS headers.
app.UseCors(FrontendCors.PolicyName);
// Establish identity before evaluating endpoint authorization policies.
app.UseAuthentication();
app.UseAuthorization();
// After authorization: anonymous calls get 401 before they can use a rate-limit permit.
app.UseRateLimiter();

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
