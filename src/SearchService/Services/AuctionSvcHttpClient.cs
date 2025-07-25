using System;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionSvcHttpClient
{
    private readonly HttpClient _httpClient;

    public AuctionSvcHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Item>> GetItemsAsync()
    {
        var lastUpdated = await DB.Find<Item, string>()
            .Sort(x => x.Descending(x => x.UpdatedAt))
            .Project(x => x.UpdatedAt.ToString())
            .ExecuteFirstAsync();

        Console.WriteLine($"Last updated date: {lastUpdated}");

        var items = await _httpClient.GetFromJsonAsync<List<Item>>("/api/auctions?date=" + lastUpdated);
        
        return items ?? new List<Item>();
    }
}
