using BiddingService.Entities;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Consumers;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> Bidding Service - Consuming AuctionCreated: " + context.Message.Id);

        var item = new Auction
        {
            ID = context.Message.Id.ToString(),
            Seller = context.Message.Seller,
            ReservePrice = context.Message.ReservePrice,
            AuctionEnd = context.Message.AuctionEnd
        };

        await item.SaveAsync();

        Console.WriteLine("--> Bidding Service - New Auction Added: " + context.Message.Id);
    }
}
