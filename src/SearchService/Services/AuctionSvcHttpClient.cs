using MongoDB.Entities;
using SearchService.Entities;
using SearchService.RequestHelpers;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
    private readonly HttpClient _httpClient;

    public AuctionSvcHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync()
    {
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        Console.WriteLine($"Last updated date: {lastUpdated}");

        var result = await _httpClient.GetFromJsonAsync<PaginatedList<Item>>("/api/auctions?date=" + lastUpdated);
        
        return result?.Results ?? new List<Item>();
    }
}
