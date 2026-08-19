namespace GameStore.Api.Authentication;

public static class AuthorizationPolicies
{
    // Keycloak realm role "Admin", mapped to the JWT "role" claim
    // (and also present in realm_access.roles).
    public const string Admin = "Admin";
    public const string AdminRole = "Admin";
}
