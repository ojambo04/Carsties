using System;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    private readonly ILogger<BidPlacedConsumer> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;


    public BidPlacedConsumer(ILogger<BidPlacedConsumer> consumerLogger, IHubContext<NotificationHub> hubContext)
    {
        _logger = consumerLogger;
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        _logger.LogInformation("==> Consuming BidPlaced message");

        await _hubContext.Clients.All.SendAsync("BidPlaced", context.Message);
    }
}
