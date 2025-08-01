using AuctionService.Data;
using Contracts;
using MassTransit;

namespace AuctionService.Contracts;

public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<AuctionFinishedConsumer> _logger;
    private readonly AuctionDbContext _dbContext;

    public BidPlacedConsumer(ILogger<AuctionFinishedConsumer> logger, AuctionDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        _logger.LogInformation("--> Consuming bid placed");

        var auction = await _dbContext.Auctions.FindAsync(context.Message.AuctionId);

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
