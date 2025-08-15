using AuctionService.Data;
using AuctionService.Entities;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly ILogger<AuctionFinishedConsumer> _logger;
    private readonly AuctionDbContext _dbContext;

    public AuctionFinishedConsumer(ILogger<AuctionFinishedConsumer> logger, AuctionDbContext context)
    {
        _logger = logger;
        _dbContext = context;
    }

    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        _logger.LogInformation("--> Consuming auction finished");

        var auctionId = Guid.Parse(context.Message.AuctionId);
        var auction = await _dbContext.Auctions.FindAsync(auctionId);

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

        auction.Status = context.Message.Amount > auction.ReservePrice
            ? Status.Finished : Status.ReserveNotMet;

        await _dbContext.SaveChangesAsync();
    }       
}
