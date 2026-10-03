using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Pikwise.Application.Users.Interfaces;
using Pikwise.Application.Users.Services;

namespace Pikwise.Api.Authentication;

public static class AuthenticationRegistration
{
    public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Authentication:Supabase:Issuer"]?.TrimEnd('/');
        var audience = configuration["Authentication:Supabase:Audience"] ?? "authenticated";
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !uri.AbsolutePath.EndsWith("/auth/v1", StringComparison.Ordinal))
            throw new InvalidOperationException("Configure Authentication:Supabase:Issuer as an HTTPS URL ending in /auth/v1.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Configure Authentication:Supabase:Audience.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                issuer + "/.well-known/jwks.json", new SupabaseJwksRetriever(issuer!),
                new HttpDocumentRetriever { RequireHttps = true })
            {
                AutomaticRefreshInterval = TimeSpan.FromMinutes(10), RefreshInterval = TimeSpan.FromMinutes(1)
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = audience,
                ValidateLifetime = true, RequireExpirationTime = true,
                RequireSignedTokens = true, ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.FromSeconds(30), NameClaimType = "sub"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var subjects = context.Principal!.FindAll("sub").ToArray();
                    var roles = context.Principal.FindAll("role").ToArray();
                    // Reject API/service keys and tokens without an unambiguous user subject.
                    if (subjects.Length != 1 || string.IsNullOrWhiteSpace(subjects[0].Value) ||
                        subjects[0].Value.Length > 128 || roles.Length != 1 || roles[0].Value != "authenticated")
                        context.Fail("A user access token is required.");
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization(options => options.AddPolicy("LocalAdmin", policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new LocalAdminRequirement())));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAuthorizationHandler, LocalAdminHandler>();
        return services;
    }
}
