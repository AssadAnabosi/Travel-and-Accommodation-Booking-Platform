using Domain.Enums;

namespace Application.Common.Models;

public record UserSearchFilter(string? Keyword, UserRole? Role, bool? IsActive);