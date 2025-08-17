using Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AuctionCreatedConsumer> _logger;

    public AuctionCreatedConsumer(IHubContext<NotificationHub> hubContext, ILogger<AuctionCreatedConsumer> consumerLogger)
    {
        _hubContext = hubContext;
        _logger = consumerLogger;
    }

    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        _logger.LogInformation("==> Consuming AuctionCreated message");
        
        await _hubContext.Clients.All.SendAsync("AuctionCreated", context.Message);
    }
}
