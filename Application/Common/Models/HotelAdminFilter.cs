using Domain.Enums;

namespace Application.Common.Models;

public record HotelAdminFilter(string? Keyword, int? CityId, HotelApprovalStatus? ApprovalStatus, Guid? OwnerId);
