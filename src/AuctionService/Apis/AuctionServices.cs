using AuctionService.Data;
using AutoMapper;
using MassTransit;

namespace AuctionService.Apis;

public record AuctionServices(
    HttpContext HttpContext,
    IAuctionRepository AuctionRepo,
    IMapper Mapper,
    IPublishEndpoint Publisher
);
