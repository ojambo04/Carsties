using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("auction-service")]
public class AuctionApiTests : IAsyncLifetime
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _httpClient;
    private const string GtId = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

    public AuctionApiTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuctions_ShouldReturnPaginatedListOfAuctions()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync("/api/auctions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paginatedList = await response.Content.ReadFromJsonAsync<PaginatedList<AuctionDto>>();
        Assert.NotNull(paginatedList);
        Assert.Equal(3, paginatedList.TotalCount);
        Assert.Equal(1, paginatedList.PageCount);
        Assert.Equal(3, paginatedList.Results.Count);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ShouldReturnAuction()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync($"/api/auctions/{GtId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuctionDto>();
        Assert.Equal("GT", result?.Model);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ShouldReturn404()
    {
        // Arrange

        // Act
        var response = await _httpClient.GetAsync($"/api/auctions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithNoAuth_ShouldReturn401()
    {
        // Arrange
        var createAuctionDto = new CreateAuctionDto();

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/api/auctions", createAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuthAndValidData_ShouldReturn201()
    {
        // Arrange
        var createAuctionDto = AuctionHelper.GetAuctionForCreate();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PostAsJsonAsync($"/api/auctions", createAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateAuction_WithAuthAndInvalidData_ShouldReturn400()
    {
        // Arrange
        var createAuctionDto = AuctionHelper.GetAuctionForCreate();
        createAuctionDto.Model = null!;
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/auctions", createAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validationProblemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(validationProblemDetails);
        Assert.True(validationProblemDetails.Errors.ContainsKey("Model"));
        Assert.Equal("The Model field is required.", validationProblemDetails.Errors["Model"][0]);
    }

    [Fact]
    public async Task UpdateAuction_WithNoAuth_ShouldReturn401()
    {
        // Arrange
        var updateAuctionDto = new UpdateAuctionDto();

         // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/auctions/{GtId}", updateAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithDataAndValidUser_ShouldReturn200()
    {
        // Arrange
        var updateAuctionDto = new UpdateAuctionDto { Model = "Updated Model" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/auctions/{GtId}", updateAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedAuction = await _httpClient.GetFromJsonAsync<AuctionDto>($"/api/auctions/{GtId}");
        Assert.NotNull(updatedAuction);
        Assert.Equal("Updated Model", updatedAuction.Model);
    }

    [Fact]
    public async Task UpdateAuction_WithDataAndInvalidUser_ShouldReturn403()
    {
        // Arrange
        var updateAuctionDto = new UpdateAuctionDto { Model = "Updated Model" };
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("not-bob"));

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/auctions/{GtId}", updateAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuction_WithAuthAndInvalidId_ShouldReturn404()
    {
        // Arrange
        var updateAuctionDto = new UpdateAuctionDto();
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/auctions/{Guid.NewGuid()}", updateAuctionDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task DeleteAuction_WithValidIdAndInValidUser_ShouldReturn403()
    {
        // Arrange
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("not-bob"));

        // Act
        var response = await _httpClient.DeleteAsync($"/api/auctions/{GtId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_WithValidIdAndValidUser_ShouldReturn200()
    {
        // Arrange
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.DeleteAsync($"/api/auctions/{GtId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAuction_WithAuthAndInvalidId_ShouldReturn404()
    {
        // Arrange
        _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

        // Act
        var response = await _httpClient.DeleteAsync($"/api/auctions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        DbHelper.ReinitDbForTests(db);
        return Task.CompletedTask;
    }
}
