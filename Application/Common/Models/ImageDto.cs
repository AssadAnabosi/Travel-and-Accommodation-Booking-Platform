namespace Application.Common.Models;

/// <summary>A hotel or room gallery image, with the id needed to remove it.</summary>
public record ImageDto(int Id, string Url, int DisplayOrder);
