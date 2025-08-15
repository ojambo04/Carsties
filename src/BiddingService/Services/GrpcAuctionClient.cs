using System;
using AuctionService.Protos;
using BiddingService.Entities;
using Grpc.Net.Client;

namespace BiddingService.Services;

public class GrpcAuctionClient
{
    private readonly ILogger<GrpcAuctionClient> _logger;
    private readonly IConfiguration _config;


    public GrpcAuctionClient(ILogger<GrpcAuctionClient> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Auction? GetAuction(string id)
    {
        _logger.LogInformation("Calling Grpc Auction Service");
        
        var channel = GrpcChannel.ForAddress(_config["GrpcAuction"]!);
        var request = new GrpcAuctionRequest { AuctionId = id };
        var client = new GrpcAuction.GrpcAuctionClient(channel);

        try
        {
            var response = client.GetAuction(request);
            var auction = new Auction
            {
                ID = response.Auction.Id,
                Seller = response.Auction.Seller,
                AuctionEnd = DateTime.Parse(response.Auction.AuctionEnd),
                ReservePrice = response.Auction.ReservePrice
            };
            
            return auction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not call Grpc server");
            return null;
        }

    }
}

