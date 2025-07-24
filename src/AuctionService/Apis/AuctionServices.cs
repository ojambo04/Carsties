using AuctionService.Data;
using AutoMapper;
using MassTransit;

namespace AuctionService.Apis;

public record AuctionServices(
    IMapper Mapper,
    AuctionDbContext Context,
    HttpContext HttpContext,
    IPublishEndpoint Publisher
);
