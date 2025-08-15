using AutoMapper;
using BiddingService.DTOs;
using BiddingService.Entities;
using BiddingService.Services;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Apis;

public static class BiddingApi
{
    public static IEndpointRouteBuilder MapBiddingApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/bids")
            .WithTags("Search");

        api.MapGet("/{id}", GetBidsForAuction)
            .WithName("GetBidsForAuction")
            .Produces<List<BidDto>>(StatusCodes.Status200OK);

        api.MapPost("/", PlaceBid)
            .WithName("PlaceBid")
            .RequireAuthorization()
            .Produces<BidDto>(StatusCodes.Status200OK);

        return app;
    }

    public static async Task<IResult> GetBidsForAuction(
        [FromRoute] string id,
        [FromServices] IMapper mapper)
    {
        var bids = await DB.Find<Bid>()
            .Match(b => b.AuctionId == id)
            .Sort(b => b.Descending(x => x.BidTime))
            .ExecuteAsync();

        return TypedResults.Ok(mapper.Map<List<BidDto>>(bids));
    }

    public static async Task<IResult> PlaceBid(
        [FromQuery] string auctionId,
        [FromQuery] int amount,
        [FromServices] IMapper mapper,
        [FromServices] IPublishEndpoint publisher,
        [FromServices] GrpcAuctionClient grpcAuction,
        HttpContext httpContext)
    {
        var auction = await DB.Find<Auction>().OneAsync(auctionId);

        if (auction == null)
        {
            auction = grpcAuction.GetAuction(auctionId);

            if (auction == null)
                return TypedResults.BadRequest("Cannot accept bids on this auction at this time");
        }

        if (auction.Seller == httpContext.User.Identity!.Name)
        {
            return TypedResults.BadRequest("Cannot bid on your own auction");
        }

        var bid = new Bid
        {
            AuctionId = auctionId,
            Bidder = httpContext.User.Identity!.Name!,
            Amount = amount
        };

        if (auction.AuctionEnd < DateTime.UtcNow)
        {
            bid.BidStatus = BidStatus.Finished;
        }
        else
        {
            var highBid = await DB.Find<Bid>()
                .Match(b => b.AuctionId == auctionId)
                .Sort(b => b.Descending(x => x.Amount))
                .ExecuteFirstAsync();

            if (highBid == null || amount > highBid.Amount)
            {
                bid.BidStatus = amount > auction.ReservePrice ?
                    BidStatus.Accepted :
                    BidStatus.AcceptedBelowReserve;
            }
            else
            {
                bid.BidStatus = BidStatus.TooLow;
            }
        }

        await DB.SaveAsync(bid);
        await publisher.Publish(mapper.Map<BidPlaced>(bid));

        return TypedResults.Ok(mapper.Map<BidDto>(bid));
    }
}
