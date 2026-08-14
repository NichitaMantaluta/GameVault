using GameStore.Api.Domain.Games;
using GameStore.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameStore.Tests;

public class GameStoreApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"GameStoreTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<GameStoreDbContext>>();
            services.RemoveAll<DbContextOptions<GameStoreDbContext>>();
            services.RemoveAll<GameStoreDbContext>();

            services.AddDbContext<GameStoreDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
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
}
