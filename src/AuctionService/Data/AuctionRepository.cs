
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionRepository : IAuctionRepository
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;

    public AuctionRepository(AuctionDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    
    public void AddAuction(Auction auction)
    {
        _context.Auctions.Add(auction);
    }

    public async Task<AuctionDto?> GetAuctionByIdAsync(Guid id)
    {
        return await _context.Auctions
            .ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Auction?> GetAuctionEntityById(Guid id)
    {
        return await _context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<PaginatedList<AuctionDto>> GetAuctionsAsync(SearchParams searchParams)
    {
        var query = _context.Auctions.AsQueryable();
        
        if (!string.IsNullOrEmpty(searchParams.Date))
        {
            var date = DateTime.Parse(searchParams.Date).ToUniversalTime();
            query = query.Where(x => x.UpdatedAt.CompareTo(date) > 0);
        }

        var pageSize = searchParams.PageSize ?? 15;
        var pageNumber = searchParams.PageNumber ?? 1;
        var totalCount = await query.CountAsync();

        var results = await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
        var result = new PaginatedList<AuctionDto>(results, pageCount, totalCount);
        
        return result;
    }

    public void RemoveAuction(Auction auction)
    {
        _context.Auctions.Remove(auction);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
