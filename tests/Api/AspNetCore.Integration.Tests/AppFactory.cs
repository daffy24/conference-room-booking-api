using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ConferenceBooking.Api.AspNetCore.Integration.Tests;

public class AppFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "http://localhost:8080/realms/conference-booking";
    private const string Audience = "conference-booking-api";
    private readonly RSA _rsa = RSA.Create(2048);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(AuthorizationProbeController).Assembly);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                metadata.SigningKeys.Add(new RsaSecurityKey(_rsa) { KeyId = "test-key" });
                // Replace only discovery; the real JWT validator and authorization middleware are exercised.
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    public string CreateToken(string? role = "Customer", string? invalidPart = null, string subject = "user-123")
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new("preferred_username", "test-customer") };
        if (invalidPart != "subject")
        {
            claims.Add(new Claim("sub", subject));
        }

        if (role is not null)
        {
            claims.Add(new Claim("roles", role));
        }

        using var untrustedRsa = RSA.Create(2048);
        var key = new RsaSecurityKey(invalidPart == "signature" ? untrustedRsa : _rsa) { KeyId = "test-key" };
        var token = new JwtSecurityToken(
            issuer: invalidPart == "issuer" ? "https://untrusted.example" : Issuer,
            audience: invalidPart == "audience" ? "another-api" : Audience,
            claims: claims,
            notBefore: invalidPart == "not-before" ? now.AddMinutes(5) : now.AddMinutes(-10),
            expires: invalidPart == "expiration" ? now.AddMinutes(-5) : now.AddMinutes(10),
            signingCredentials: invalidPart == "unsigned" ? null : new SigningCredentials(key, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _rsa.Dispose();
        }
    }
}
