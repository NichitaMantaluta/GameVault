using GameStore.Api.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Persistence;

public class GameStoreDbContext(DbContextOptions<GameStoreDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genres");
            entity.HasKey(genre => genre.Id);
            entity.Property(genre => genre.Id).ValueGeneratedOnAdd();
            entity.Property(genre => genre.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("Games");
            entity.HasKey(game => game.Id);
            entity.Property(game => game.Name).IsRequired().HasMaxLength(200);
            entity.Property(game => game.Description).IsRequired().HasMaxLength(4000);
            entity.Property(game => game.Price).HasPrecision(18, 2);
            entity.Property(game => game.IsActive).IsRequired();
            entity.Property(game => game.CreatedAt).IsRequired();
            entity.Property(game => game.UpdatedAt).IsRequired();

            entity.HasOne<Genre>()
                .WithMany()
                .HasForeignKey(game => game.GenreId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
