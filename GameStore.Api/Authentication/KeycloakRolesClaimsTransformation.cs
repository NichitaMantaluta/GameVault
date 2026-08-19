using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace GameStore.Api.Authentication;

public sealed class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        foreach (var realmAccess in identity.FindAll("realm_access"))
        {
            AddRolesFromRealmAccess(identity, realmAccess.Value);
        }

        return Task.FromResult(principal);
    }

    private static void AddRolesFromRealmAccess(ClaimsIdentity identity, string? realmAccess)
    {
        if (string.IsNullOrWhiteSpace(realmAccess) || realmAccess[0] != '{')
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles) ||
            roles.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var role in roles.EnumerateArray())
        {
            var roleName = role.GetString();
            if (string.IsNullOrWhiteSpace(roleName) || identity.HasClaim("role", roleName))
            {
                continue;
            }

            identity.AddClaim(new Claim("role", roleName));
        }
    }
}
