using GameStore.Api.Domain.Carts;
using GameStore.Api.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Persistence;

public class GameStoreDbContext(DbContextOptions<GameStoreDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

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
            entity.Property(game => game.ImageUrl).HasMaxLength(2048);
            entity.Property(game => game.IsActive).IsRequired();
            entity.Property(game => game.CreatedAt).IsRequired();
            entity.Property(game => game.UpdatedAt).IsRequired();

            entity.HasOne<Genre>()
                .WithMany()
                .HasForeignKey(game => game.GenreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(cart => cart.Id);
            entity.Property(cart => cart.UserId).IsRequired().HasMaxLength(256);
            entity.HasIndex(cart => cart.UserId).IsUnique();
            entity.HasMany(cart => cart.Items)
                .WithOne()
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems", table =>
                table.HasCheckConstraint("CK_CartItems_Quantity", "\"Quantity\" >= 1"));
            entity.HasKey(item => new { item.CartId, item.GameId });
            entity.Property(item => item.Quantity).IsRequired();
            entity.HasOne<Game>()
                .WithMany()
                .HasForeignKey(item => item.GameId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
