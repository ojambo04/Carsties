namespace AuctionService.DTOs;

public class SearchParams
{
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public string? Date { get; set; }
}
