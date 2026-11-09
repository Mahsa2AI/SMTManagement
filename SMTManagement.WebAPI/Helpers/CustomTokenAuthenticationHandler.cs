using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SMTManagement.WebAPI.Helpers;

public class CustomTokenAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public CustomTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = header.ToString();

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(7).Trim();
        }

        var configuredToken = _configuration["ApiAuthentication:Token"];

        if (string.IsNullOrEmpty(configuredToken) ||
            !string.Equals(token, configuredToken, StringComparison.Ordinal))
        {
            Logger.LogWarning(
                "API authentication failed because the bearer token was invalid. Path: {Path}",
                Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API token"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "API User")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}