using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Pikwise.Api.Authentication;

namespace Pikwise.IntegrationTests;

// Tests sign real JWTs with temporary keys; the production Bearer handler still validates them.
internal sealed class AuthTestTokens : IDisposable
{
    public const string Issuer = "https://auth.example.test/auth/v1";
    private readonly ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly RSA rsaKey = RSA.Create(2048);
    private readonly ECDsa publicKey;
    private readonly RSA rsaPublicKey;
    public string Subject { get; } = Guid.NewGuid().ToString();
    public string OtherSubject { get; } = Guid.NewGuid().ToString();
    public string Jwks { get; }

    public AuthTestTokens()
    {
        publicKey = ECDsa.Create(signingKey.ExportParameters(false));
        rsaPublicKey = RSA.Create(rsaKey.ExportParameters(false));
        var ec = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(new ECDsaSecurityKey(publicKey) { KeyId = "ec-test" });
        var rsa = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(rsaPublicKey) { KeyId = "rsa-test" });
        Jwks = System.Text.Json.JsonSerializer.Serialize(new { keys = new[] { ec, rsa } });
    }

    public string Create(string variant = "valid", string? subject = null)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new JwtPayload
        {
            ["iss"] = variant == "issuer" ? "https://other.example.test/auth/v1" : Issuer,
            ["aud"] = variant == "audience" ? "anon" : "authenticated",
            ["sub"] = subject ?? Subject,
            ["email"] = variant == "invalid-email" ? "invalid" : "user@example.test",
            ["role"] = variant == "service-role" ? "service_role" : "authenticated",
            ["nbf"] = now.AddMinutes(variant == "future" ? 5 : -10).ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(variant == "expired" ? -5 : 5).ToUnixTimeSeconds()
        };
        if (variant == "no-sub") payload.Remove("sub");
        if (variant == "blank-sub") payload["sub"] = " ";
        if (variant == "long-sub") payload["sub"] = new string('a', 129);
        if (variant == "no-exp") payload.Remove("exp");
        if (variant == "no-email") payload.Remove("email");
        if (variant == "metadata-admin") payload["user_metadata"] = new Dictionary<string, object> { ["role"] = "Admin" };
        if (variant == "duplicate-sub") payload["sub"] = new[] { Subject, OtherSubject };

        SigningCredentials? credentials = variant == "unsigned" ? null : variant == "rsa"
            ? new(new RsaSecurityKey(rsaKey) { KeyId = "rsa-test" }, SecurityAlgorithms.RsaSha256)
            : new(new ECDsaSecurityKey(signingKey) { KeyId = "ec-test" }, SecurityAlgorithms.EcdsaSha256);
        if (variant == "hs256")
            credentials = new(new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)), SecurityAlgorithms.HmacSha256);
        if (variant == "signature")
        {
            using var wrongKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                new JwtHeader(new SigningCredentials(new ECDsaSecurityKey(wrongKey) { KeyId = "ec-test" }, SecurityAlgorithms.EcdsaSha256)), payload));
        }
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
    }

    public WebApplicationFactory<Program> Configure(PikwiseApiFactory factory, Action<IServiceCollection>? replaceServices = null) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            // Replace only JWKS transport. Issuer, audience, lifetime and signature checks remain enabled.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    Issuer + "/.well-known/jwks.json", new SupabaseJwksRetriever(Issuer), new TestDocuments(Jwks)));
            replaceServices?.Invoke(services);
        }));

    public void Dispose()
    {
        signingKey.Dispose(); rsaKey.Dispose(); publicKey.Dispose(); rsaPublicKey.Dispose();
    }

    private sealed class TestDocuments(string jwks) : IDocumentRetriever
    {
        public Task<string> GetDocumentAsync(string address, CancellationToken cancellationToken)
        {
            Assert.Equal(Issuer + "/.well-known/jwks.json", address);
            return Task.FromResult(jwks);
        }
    }
}
