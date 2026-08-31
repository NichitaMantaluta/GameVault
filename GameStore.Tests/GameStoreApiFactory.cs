using GameStore.Api.Domain.Games;
using GameStore.Api.Integrations.Payments;
using GameStore.Api.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Tests;

public class GameStoreApiFactory : WebApplicationFactory<Program>
{
    public const string StripeWebhookSecret = "whsec_test_gamestore";

    private readonly string _databaseName = $"GameStoreTests-{Guid.NewGuid()}";
    private readonly FakePaymentService _paymentService = new();

    public FakePaymentService PaymentService => _paymentService;

    public GameStoreApiFactory()
    {
        Environment.SetEnvironmentVariable("Authentication__Authority", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Authentication__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Authentication__RequireHttpsMetadata", "false");
        Environment.SetEnvironmentVariable("Stripe__WebhookSecret", StripeWebhookSecret);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Authority", TestJwt.Issuer);
        builder.UseSetting("Authentication:Audience", TestJwt.Audience);
        builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
        builder.UseSetting("Stripe:WebhookSecret", StripeWebhookSecret);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<GameStoreDbContext>>();
            services.RemoveAll<DbContextOptions<GameStoreDbContext>>();
            services.RemoveAll<GameStoreDbContext>();
            services.RemoveAll<IPaymentService>();

            services.AddDbContext<GameStoreDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<IPaymentService>(_paymentService);

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = TestJwt.Issuer;
                options.Audience = TestJwt.Audience;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.RefreshOnIssuerKeyNotFound = false;
                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = TestJwt.Issuer
                };
                options.Configuration.SigningKeys.Add(TestJwt.SigningKey);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = TestJwt.Issuer,
                    ValidAudience = TestJwt.Audience,
                    IssuerSigningKey = TestJwt.SigningKey,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
                };
            });
        });
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        TestJwt.AuthenticateAsAdmin(client);
        return client;
    }

    public HttpClient CreateCustomerClient()
    {
        var client = CreateClient();
        TestJwt.AuthenticateAsCustomer(client);
        return client;
    }

    public HttpClient CreateUserClient(string userId, params string[] roles)
    {
        var client = CreateClient();
        TestJwt.AuthenticateAsUser(client, userId, roles);
        return client;
    }

    public async Task<int> SeedGenreAsync(string name = "Platformer")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var genre = new Genre { Name = name };
        db.Genres.Add(genre);
        await db.SaveChangesAsync();
        return genre.Id;
    }

    public async Task<Game> SeedGameAsync(
        int genreId,
        string name = "Super Mario Bros. 3",
        string description = "A classic platform game.",
        decimal price = 19.99m,
        bool isActive = true,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        string? imageUrl = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var timestamp = createdAt ?? DateTimeOffset.UtcNow;
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            ImageUrl = imageUrl,
            GenreId = genreId,
            CreatedAt = timestamp,
            UpdatedAt = updatedAt ?? timestamp,
            IsActive = isActive
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }
}
