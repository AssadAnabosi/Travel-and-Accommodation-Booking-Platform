namespace Domain.Exceptions;

/// <summary>The image id doesn't belong to the hotel/room it was addressed through. Surfaces as 404.</summary>
public sealed class ImageNotFoundException(int imageId, string owner)
    : DomainException($"Image {imageId} does not belong to this {owner}.");
