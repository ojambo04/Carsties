using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Entities;

namespace SearchService.Consumers;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly ILogger<AuctionFinishedConsumer> _logger;

    public AuctionFinishedConsumer(ILogger<AuctionFinishedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        _logger.LogInformation("--> Consuming auction finished");

        var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

        if (auction == null)
        {
            _logger.LogWarning($"Auction with ID {context.Message.AuctionId} not found.");
            return;
        }

        if (context.Message.ItemSold)
        {
            auction.Winner = context.Message.Winner;
            auction.SoldAmount = context.Message.Amount;
        }

        auction.Status = "Finished";

        await auction.SaveAsync();
    }
}
