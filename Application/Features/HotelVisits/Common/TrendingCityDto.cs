namespace Application.Features.HotelVisits.Common;

public record TrendingCityDto(int CityId, string CityName, string? ThumbnailUrl, int VisitCount);