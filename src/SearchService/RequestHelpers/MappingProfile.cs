using AutoMapper;
using Contracts;
using SearchService.Entities;

namespace SearchService.RequestHelpers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<AuctionCreated, Item>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.Id));
        CreateMap<AuctionUpdated, Item>();
    }
}