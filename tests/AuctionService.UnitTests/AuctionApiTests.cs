using System.Security.Claims;
using AuctionService.Apis;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionApiTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly Fixture _fixture;
    private readonly AuctionServices _auctionServices;
    private readonly ClaimsPrincipal _user;

    public AuctionApiTests()
    {
        _auctionRepo = new Mock<IAuctionRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();   

        _fixture = new Fixture();

        // Remove the ThrowingRecursionBehavior (the default)
        _fixture.Behaviors
            .OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));

        // Add OmitOnRecursionBehavior so AutoFixture will set recursive / circular properties to null
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var mockMapper = new MapperConfiguration(mc =>
        {
            mc.AddMaps(typeof(MappingProfile).Assembly);
        }).CreateMapper().ConfigurationProvider;

        _mapper = new Mapper(mockMapper);

        _user = new ClaimsPrincipal(new ClaimsIdentity([ new(ClaimTypes.Name, "test")], "testing"));

        var httpContext = new DefaultHttpContext()
        {
            User = _user
        };

        _auctionServices = new AuctionServices(httpContext, _auctionRepo.Object, _mapper, _publishEndpoint.Object);
    }

    [Fact]
    public async Task GetAuctions_WithSearchParams_ReturnsOkResult()
    {
        // Arrange
        var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();

        var paginatedList = new PaginatedList<AuctionDto>(auctions, 1, 10);
        var searchParams = new SearchParams { PageNumber = 1, PageSize = 10 };

        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(searchParams))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await AuctionApi.GetAuctions(_auctionServices, searchParams); 


        // Assert
        var okResult = Assert.IsType<Ok<PaginatedList<AuctionDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(10, okResult.Value.Results.Count);
        Assert.Equal(1, okResult.Value.PageCount);
        Assert.Equal(10, okResult.Value.TotalCount);
    }

    [Fact]
    public async Task GetAuctions_WithNoResults_ReturnsOkWithEmptyResult()
    {
        // Arrange
        var emptyList = new PaginatedList<AuctionDto>([], 0, 0);
        var searchParams = new SearchParams();

        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(searchParams))
            .ReturnsAsync(emptyList);

        // Act
        var result = await AuctionApi.GetAuctions(_auctionServices, searchParams);

        // Assert
        var okResult = Assert.IsType<Ok<PaginatedList<AuctionDto>>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Empty(okResult.Value.Results);
        Assert.Equal(0, okResult.Value.TotalCount);
    }

    [Fact]
    public async Task GetAuctionById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var auction = _fixture.Create<AuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);

        // Act
        var result = await AuctionApi.GetAuctionById(_auctionServices, Guid.NewGuid());

        // Assert
        var okResult = Assert.IsType<Ok<AuctionDto>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(auction.Id, okResult.Value.Id);
    }

    [Fact]
    public async Task GetAuctionById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(value: null);

        // Act
        var result = await AuctionApi.GetAuctionById(_auctionServices, Guid.NewGuid());

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task CreateAuction_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var createAuctionDto = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await AuctionApi.CreateAuction(_auctionServices, createAuctionDto);

        // Assert
        var createdResult = Assert.IsType<Created<AuctionDto>>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal("test", createdResult.Value.Seller);
        _auctionRepo.Verify(repo => repo.AddAuction(It.IsAny<Auction>()), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<AuctionCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAuction_WhenSaveChangesFails_ReturnsBadRequest()
    {
        // Arrange
        var createAuctionDto = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await AuctionApi.CreateAuction(_auctionServices, createAuctionDto);

        // Assert
        Assert.IsType<BadRequest<string>>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().With(a => a.Seller, "test").Create();
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await AuctionApi.UpdateAuction(_auctionServices, auction.Id, updateDto);

        // Assert
        Assert.IsType<Ok<AuctionDto>>(result);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<AuctionUpdated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(value: null);

        // Act
        var result = await AuctionApi.UpdateAuction(_auctionServices, Guid.NewGuid(), updateDto);

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task UpdateAuction_WithInvalidUser_ReturnsForbid()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().With(a => a.Seller, "not-test").Create();
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);

        // Act
        var result = await AuctionApi.UpdateAuction(_auctionServices, auction.Id, updateDto);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);
    }

    [Fact]
    public async Task UpdateAuction_WhenSaveChangesFails_ReturnsBadRequest()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().With(a => a.Seller, "test").Create();
        var updateDto = _fixture.Create<UpdateAuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await AuctionApi.UpdateAuction(_auctionServices, auction.Id, updateDto);

        // Assert
        Assert.IsType<BadRequest<string>>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().Without(a => a.Item).With(a => a.Seller, "test").Create();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

        // Act
        var result = await AuctionApi.DeleteAuctionById(_auctionServices, auction.Id);

        // Assert
        Assert.IsType<Ok>(result);
        _publishEndpoint.Verify(p => p.Publish(It.IsAny<AuctionDeleted>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(value: null);

        // Act
        var result = await AuctionApi.DeleteAuctionById(_auctionServices, Guid.NewGuid());

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task DeleteAuction_WithInvalidUser_ReturnsForbid()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().With(a => a.Seller, "not-test").Create();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);

        // Act
        var result = await AuctionApi.DeleteAuctionById(_auctionServices, auction.Id);

        // Assert
        Assert.IsType<ForbidHttpResult>(result);
    }

    [Fact]
    public async Task DeleteAuction_WhenSaveChangesFails_ReturnsBadRequest()
    {
        // Arrange
        var auction = _fixture.Build<Auction>().With(a => a.Seller, "test").Create();
        _auctionRepo.Setup(repo => repo.GetAuctionEntityById(It.IsAny<Guid>())).ReturnsAsync(auction);
        _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

        // Act
        var result = await AuctionApi.DeleteAuctionById(_auctionServices, auction.Id);

        // Assert
        Assert.IsType<BadRequest<string>>(result);
    }
}