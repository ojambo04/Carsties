using MongoDB.Entities;

namespace BiddingService.Entities;

public class Auction : Entity
{
    public required DateTime AuctionEnd { get; set; }
    public required string Seller { get; set; }
    public required int ReservePrice { get; set; }
    public bool Finished { get; set; }
}
