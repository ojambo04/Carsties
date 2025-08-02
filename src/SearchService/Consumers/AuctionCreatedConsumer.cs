using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Entities;

namespace SearchService.Consumers;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;

    public AuctionCreatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("--> Consuming AuctionCreated: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        if (item.Model == "Foo")
            throw new ArgumentException("Cannot sell cars with name of Foo");

        await item.SaveAsync();

        Console.WriteLine("--> New Auction Added to Mongo DB: " + context.Message.Id);
    }
}
