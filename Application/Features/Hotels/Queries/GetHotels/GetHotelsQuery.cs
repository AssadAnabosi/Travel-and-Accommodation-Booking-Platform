using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Hotels.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Hotels.Queries.GetHotels;

/// <summary>Admin grid: every hotel in any approval state, optionally filtered.</summary>
[Authorize(Roles = "Admin")]
public record GetHotelsQuery(
    string? Keyword,
    int? CityId,
    HotelApprovalStatus? ApprovalStatus,
    Guid? OwnerId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<HotelDto>>;
