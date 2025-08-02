using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.Filters;
using AutoMapper.QueryableExtensions;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Apis;

public static class AuctionApi
{
	public static IEndpointRouteBuilder MapAuctionApi(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/auctions")
			.WithTags("Auctions")
			.AddEndpointFilter<ValidationFilter>(); ;

		api.MapGet("/", GetAuctions)
			.WithName("GetAuctions")
			.Produces<PaginatedList<AuctionDto>>(StatusCodes.Status200OK);

		api.MapGet("/{id}", GetAuctionById)
			.WithName("GetAuctionsById")
			.Produces<AuctionDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound);

		api.MapPost("/", CreateAuction)
			.WithName("CreateAuction")
			.RequireAuthorization()
			.Produces<AuctionDto>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status401Unauthorized);

		api.MapPut("/{id}", UpdateAuction)
			.WithName("UpdateAuction")
			.RequireAuthorization()
			.Produces<AuctionDto>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status403Forbidden);

		api.MapDelete("/{id}", DeleteAuctionById)
			.WithName("DeleteAuctionById")
			.RequireAuthorization()
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status401Unauthorized);

		return app;
	}

	private static async Task<IResult> GetAuctions(
		[AsParameters] AuctionServices service,
		[AsParameters] SearchParams searchParams)
	{
		var query = service.Context.Auctions.AsQueryable();

		var pageSize = searchParams.PageSize ?? 15;
		var pageNumber = searchParams.PageNumber ?? 1;
		var totalCount = await query.CountAsync();

		var results = await query.ProjectTo<AuctionDto>(service.Mapper.ConfigurationProvider)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
		var result = new PaginatedList<AuctionDto>(results, pageCount, totalCount);

		return Results.Ok(result);
	}

	private static async Task<IResult> GetAuctionById(
		[AsParameters] AuctionServices service,
		[FromRoute] Guid id)
	{
		var auction = await service.Context.Auctions
			.Include(x => x.Item)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (auction == null)
		{
			return Results.NotFound();
		}

		var auctionDto = service.Mapper.Map<AuctionDto>(auction);
		return Results.Ok(auctionDto);
	}

	public static async Task<IResult> CreateAuction(
		[AsParameters] AuctionServices services,
		[FromBody] CreateAuctionDto dto)
	{
		var auction = services.Mapper.Map<Auction>(dto);
		auction.Seller = services.HttpContext.User.Identity!.Name!;

		services.Context.Auctions.Add(auction);

		var auctionDto = services.Mapper.Map<AuctionDto>(auction);
		await services.Publisher.Publish(services.Mapper.Map<AuctionCreated>(auctionDto));

		var result = await services.Context.SaveChangesAsync() > 0;

		return result ?
			Results.Created($"/api/auctions/{auction.Id}", auctionDto) :
			Results.BadRequest("Failed to create auction");
	}

	public static async Task<IResult> UpdateAuction(
		[AsParameters] AuctionServices services,
		[FromRoute] Guid id,
		[FromBody] UpdateAuctionDto dto)
	{
		var auction = await services.Context.Auctions
			.Include(x => x.Item)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (auction is null) return Results.NotFound();

		if (auction.Seller != services.HttpContext.User.Identity!.Name)
		{
			return Results.Forbid();
		}

		auction.Item!.Make = dto.Make ?? auction.Item.Make;
		auction.Item!.Model = dto.Model ?? auction.Item.Model;
		auction.Item!.Year = dto.Year ?? auction.Item.Year;
		auction.Item!.Color = dto.Color ?? auction.Item.Color;
		auction.Item!.Mileage = dto.Mileage ?? auction.Item.Mileage;

		var auctionDto = services.Mapper.Map<AuctionDto>(auction);

		// Check if any changes were made
		if (!services.Context.ChangeTracker.HasChanges())
		{
			return Results.Ok(auctionDto);
		}

		await services.Publisher.Publish(services.Mapper.Map<AuctionUpdated>(auctionDto));

		var result = await services.Context.SaveChangesAsync() > 0;

		return result ?
			Results.Ok(auctionDto) :
			Results.BadRequest("Failed to update auction");
	}

	private static async Task<IResult> DeleteAuctionById(
		[AsParameters] AuctionServices services,
		[FromRoute] Guid id)
	{
		var auction = await services.Context.Auctions
			.FirstOrDefaultAsync(x => x.Id == id);

		if (auction == null) return Results.NotFound();

		if (auction.Seller != services.HttpContext.User.Identity!.Name)
		{
			return Results.Forbid();
		}

		services.Context.Auctions.Remove(auction);

		await services.Publisher.Publish(new AuctionDeleted { Id = auction.Id.ToString() });

		await services.Context.SaveChangesAsync();

		return Results.Ok();
	}
}
