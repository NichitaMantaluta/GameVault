using GameStore.Api.Authentication;
using GameStore.Api.Features.Cart.AddCartItem;
using GameStore.Api.Features.Cart.GetCart;
using GameStore.Api.Features.Cart.RemoveCartItem;
using GameStore.Api.Features.Cart.UpdateCartItem;
using GameStore.Api.Features.Games.CreateGame;
using GameStore.Api.Features.Games.DeleteGame;
using GameStore.Api.Features.Games.GetGame;
using GameStore.Api.Features.Games.GetGames;
using GameStore.Api.Features.Games.UpdateGame;
using GameStore.Api.Persistence;
using Microsoft.EntityFrameworkCore;

LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
LoadDotEnv(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")));
EnsureGameStoreConnectionString();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration);
builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GameStore")));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        db.Database.Migrate();
    }
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapCreateGame();
app.MapGetGame();
app.MapGetGames();
app.MapUpdateGame();
app.MapDeleteGame();
app.MapGetCart();
app.MapAddCartItem();
app.MapUpdateCartItem();
app.MapRemoveCartItem();

app.Run();

static void LoadDotEnv(string path)
{
    if (!File.Exists(path))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

static void EnsureGameStoreConnectionString()
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__GameStore")))
    {
        return;
    }

    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
    var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
    var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var connectionString =
        $"Host={host};Port={port ?? "5432"};Database={database};Username={username};Password={password}";

    Environment.SetEnvironmentVariable("ConnectionStrings__GameStore", connectionString);
}

public partial class Program;
