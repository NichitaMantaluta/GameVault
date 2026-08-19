using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Api.Authentication;

public static class KeycloakAuthenticationExtensions
{
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"];
        var audience = configuration["Authentication:Audience"];

        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Authentication:Authority and Authentication:Audience must be configured.");
        }

        var requireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidAudiences = [audience, "account"];
                options.TokenValidationParameters.ValidIssuer = authority;
                options.TokenValidationParameters.AudienceValidator =
                    (audiences, token, _) => HasExpectedAudience(audiences, token, audience);
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GameStore.Api.Authentication");
                        logger.LogWarning(context.Exception, "JWT authentication failed.");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GameStore.Api.Authentication");
                        var claimTypes = string.Join(
                            ", ",
                            context.Principal?.Claims.Select(claim => claim.Type) ?? []);
                        logger.LogInformation(
                            "JWT validated for {Path}. Claims: {ClaimTypes}",
                            context.Request.Path,
                            claimTypes);
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GameStore.Api.Authentication");
                        var hasAuthorization = context.Request.Headers.ContainsKey("Authorization");
                        var detail = context.AuthenticateFailure?.Message
                            ?? context.ErrorDescription
                            ?? (hasAuthorization
                                ? "The access token was rejected."
                                : "No access token was sent.");

                        logger.LogWarning(
                            "JWT challenge for {Method} {Path}. Authorization header present: {HasAuthorization}. {Detail}",
                            context.Request.Method,
                            context.Request.Path,
                            hasAuthorization,
                            detail);

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            title = "Unauthorized",
                            detail
                        });
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireRole(AuthorizationPolicies.AdminRole));

        services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

        return services;
    }

    private static bool HasExpectedAudience(
        IEnumerable<string>? audiences,
        SecurityToken token,
        string expectedAudience)
    {
        var values = audiences as ICollection<string> ?? audiences?.ToArray() ?? [];

        if (values.Any(value => string.Equals(value, expectedAudience, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (string.Equals(GetAuthorizedParty(token), expectedAudience, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Keycloak public-client access tokens commonly use aud=account and azp=<clientId>.
        return values.Any(value => string.Equals(value, "account", StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetAuthorizedParty(SecurityToken token)
    {
        if (token is JsonWebToken jsonWebToken)
        {
            if (!string.IsNullOrWhiteSpace(jsonWebToken.Azp))
            {
                return jsonWebToken.Azp;
            }

            if (jsonWebToken.TryGetValue("azp", out string azp) && !string.IsNullOrWhiteSpace(azp))
            {
                return azp;
            }
        }

        return null;
    }
}


