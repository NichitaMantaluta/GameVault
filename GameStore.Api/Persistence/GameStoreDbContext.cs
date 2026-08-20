using GameStore.Api.Domain.Carts;
using GameStore.Api.Domain.Games;
using GameStore.Api.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Persistence;

public class GameStoreDbContext(DbContextOptions<GameStoreDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

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

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.UserId).IsRequired().HasMaxLength(256);
            entity.Property(order => order.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.Currency).IsRequired().HasMaxLength(3);
            entity.Property(order => order.StripeCheckoutSessionId).HasMaxLength(200);
            entity.Property(order => order.CreatedAt).IsRequired();
            entity.Property(order => order.UpdatedAt).IsRequired();
            entity.HasIndex(order => order.UserId);
            entity.HasIndex(order => order.StripeCheckoutSessionId)
                .IsUnique()
                .HasFilter("\"StripeCheckoutSessionId\" IS NOT NULL");
            entity.HasMany(order => order.Items)
                .WithOne()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UserId).IsRequired().HasMaxLength(256);
            entity.Property(item => item.GameName).IsRequired().HasMaxLength(200);
            entity.Property(item => item.Price).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.OrderId, item.GameId }).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.GameId }).IsUnique();
            entity.HasOne<Game>()
                .WithMany()
                .HasForeignKey(item => item.GameId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
