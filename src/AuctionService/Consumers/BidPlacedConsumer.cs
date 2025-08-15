using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<BidPlacedConsumer> _logger;
    private readonly AuctionDbContext _dbContext;

    public BidPlacedConsumer(ILogger<BidPlacedConsumer> logger, AuctionDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        _logger.LogInformation("--> Consuming bid placed");

        var auctionId = Guid.Parse(context.Message.AuctionId);
        var auction = await _dbContext.Auctions.FindAsync(auctionId);

        if (auction == null)
        {
            _logger.LogWarning($"Auction with ID {context.Message.AuctionId} not found.");
            return;
        }

        if (context.Message.BidStatus.Contains("Accepted") &&
            context.Message.Amount > (auction.CurrentHighBid ?? 0))
        {
            auction.CurrentHighBid = context.Message.Amount;
            await _dbContext.SaveChangesAsync();
        }
    }
}
