using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Entities;

namespace SearchService.Consumers;

public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<AuctionFinishedConsumer> _logger;

    public BidPlacedConsumer(ILogger<AuctionFinishedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        _logger.LogInformation("--> Consuming bid placed");

        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

        if (auction == null)
        {
            _logger.LogWarning($"Auction with ID {context.Message.AuctionId} not found.");
            return;
        }

        if (context.Message.BidStatus.Contains("Accepted") &&
            context.Message.Amount > (auction.CurrentHighBid ?? 0))
        {
            auction.CurrentHighBid = context.Message.Amount;
            await auction.SaveAsync();
        }
    }
}
