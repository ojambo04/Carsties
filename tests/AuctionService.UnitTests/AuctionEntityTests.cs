using AuctionService.Entities;

namespace AuctionService.UnitTests;

public class AuctionEntityTests
{
    [Fact]
    public void HasReservePrice_ReservePriceGtZero_True()
    {
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 10 };

        var hasReservePrice = auction.HasReservePrice();

        Assert.True(hasReservePrice);
    }

    [Fact]
    public void HasReservePrice_ReservePriceZero_False()
    {
        var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 0 };

        var hasReservePrice = auction.HasReservePrice();

        Assert.False(hasReservePrice);
    }
}
