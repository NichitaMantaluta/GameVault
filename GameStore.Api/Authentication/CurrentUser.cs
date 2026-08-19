using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameStore.Api.Authentication;

internal static class CurrentUser
{
    public static string? GetId(ClaimsPrincipal user)
    {
        foreach (var claim in user.Claims)
        {
            if (IsSubjectClaim(claim.Type) && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value;
            }
        }

        var preferredUsername = user.FindFirstValue("preferred_username");
        if (!string.IsNullOrWhiteSpace(preferredUsername))
        {
            return preferredUsername;
        }

        var name = user.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static bool IsSubjectClaim(string type)
    {
        return type is "sub" or "nameid"
            || type.Equals(JwtRegisteredClaimNames.Sub, StringComparison.OrdinalIgnoreCase)
            || type.Equals(ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase);
    }
}
