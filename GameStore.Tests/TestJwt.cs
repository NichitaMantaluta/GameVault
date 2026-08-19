using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Tests;

internal static class TestJwt
{
    public const string Issuer = "http://gamestore-tests/realms/GameStore";
    public const string Audience = "gamestore";

    public static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("GameStore-Tests-Signing-Key-32b!"));

    public static string CreateAccessToken(
        IReadOnlyList<string> roles,
        string username,
        string? userId = null)
    {
        var claims = new List<Claim>
        {
            new("preferred_username", username),
            new("sub", userId ?? username),
            new("realm_access", JsonSerializer.Serialize(new { roles }))
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateAdminToken() => CreateAccessToken(["Admin"], "admin");

    public static string CreateCustomerToken() =>
        CreateAccessToken(["offline_access"], "customer");

    public static void AuthenticateAsAdmin(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAdminToken());
    }

    public static void AuthenticateAsCustomer(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateCustomerToken());
    }

    public static void AuthenticateAsUser(HttpClient client, string userId, params string[] roles)
    {
        var assignedRoles = roles.Length > 0 ? roles : ["offline_access"];
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateAccessToken(assignedRoles, userId, userId));
    }
}
