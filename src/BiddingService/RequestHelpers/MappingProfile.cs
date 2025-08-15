using AutoMapper;
using BiddingService.DTOs;
using BiddingService.Entities;
using Contracts;

namespace BiddingService.RequestHelpers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Bid, BidDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));

        CreateMap<Bid, BidPlaced>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID));
    }
}
