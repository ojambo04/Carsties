namespace AuctionService.DTOs;

public class PaginatedList<T>
{
    public IReadOnlyList<T> Results { get; set; }
    public int PageCount { get; set; }
    public long TotalCount { get; set; }

    public PaginatedList(IReadOnlyList<T> results, int pageCount, long totalCount)
    {
        Results = results;
        PageCount = pageCount;
        TotalCount = totalCount;
    }
}