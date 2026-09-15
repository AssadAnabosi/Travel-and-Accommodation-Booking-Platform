namespace Application.Features.Cities.Common;

public record CityDto(
    int Id,
    string Name,
    string Country,
    string PostOffice,
    int HotelsCount,
    DateTime CreatedAt,
    DateTime? ModifiedAt);