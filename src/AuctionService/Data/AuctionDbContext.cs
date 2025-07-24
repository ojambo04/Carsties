using AuctionService.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Auction> Auctions { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        
        modelBuilder.Entity<Auction>()
            .Property(a => a.UpdatedAt)
            .HasColumnType("timestamp(3) with time zone"); // or "timestamp(3)" if you don't use time zone

        modelBuilder.Entity<Auction>()
            .Property(a => a.CreatedAt)
            .HasColumnType("timestamp(3) with time zone");
    }
}
