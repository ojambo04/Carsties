using Grpc.Core;
using AuctionService.Data;
using AuctionService.Protos;

namespace AuctionService.Services;

public class GrpcAuctionService : GrpcAuction.GrpcAuctionBase
{
    private readonly IAuctionRepository _repository;
    private readonly ILogger<GrpcAuctionService> _logger;

    public GrpcAuctionService(ILogger<GrpcAuctionService> logger, IAuctionRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public override async Task<GrpcAuctionResponse> GetAuction(GrpcAuctionRequest request, ServerCallContext context)
    {
        _logger.LogInformation("==> Get auction Grpc request");

        var auction = await _repository.GetAuctionEntityById(Guid.Parse(request.AuctionId));

        if (auction == null) throw new RpcException(new Status(StatusCode.NotFound, "Auction not found"));

        var response = new GrpcAuctionResponse
        {
            Auction = new GrpcAuctionModel
            {
                Id = auction.Id.ToString(),
                Seller = auction.Seller,
                AuctionEnd = auction.AuctionEnd.ToString(),
                ReservePrice = auction.ReservePrice
            }
        };

        return response;
    }
}
