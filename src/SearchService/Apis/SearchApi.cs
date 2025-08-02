using MongoDB.Entities;
using SearchService.Entities;
using SearchService.RequestHelpers;

namespace SearchService.Apis;

public static class SearchApi
{
    public static IEndpointRouteBuilder MapSearchApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/search")
            .WithTags("Search");

        api.MapGet("/", GetItems)
            .WithName("GetItems")
            .Produces<PaginatedList<Item>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> GetItems([AsParameters] SearchParams searchParams)
    {
        var pageNumber = searchParams.PageNumber ?? 1;
        var pageSize = searchParams.PageSize ?? 4;

        var query = DB.PagedSearch<Item, Item>();

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
        }

        query = searchParams.OrderBy switch
        {
            "make" => query.Sort(x => x.Ascending(a => a.Make))
                .Sort(x => x.Ascending(a => a.Model)),
            "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
            _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
        };

        query = searchParams.FilterBy switch
        {
            "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
            "endingSoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6)
                && x.AuctionEnd > DateTime.UtcNow),
            _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)
        };

        if (!string.IsNullOrEmpty(searchParams.Seller))
        {
            query.Match(x => x.Seller == searchParams.Seller);
        }

        if (!string.IsNullOrEmpty(searchParams.Winner))
        {
            query.Match(x => x.Winner == searchParams.Winner);
        }

        query.PageNumber(pageNumber);
        query.PageSize(pageSize);

        var result = await query.ExecuteAsync();

        var paginatedList = new PaginatedList<Item>(
            result.Results,
            result.PageCount,
            result.TotalCount
        );

        return Results.Ok(paginatedList);
    }
}
