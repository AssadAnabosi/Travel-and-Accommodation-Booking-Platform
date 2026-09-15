using Application.Common.Security;
using Application.Features.Reviews.Common;
using MediatR;

namespace Application.Features.Reviews.Commands.CreateReview;

// No Roles restriction — a HotelOwner or Admin can also be a genuine guest at a *different*
// hotel, so eligibility is purely "did you complete a stay here," not your platform role.
[Authorize]
public record CreateReviewCommand(int HotelId, int Rating, string? Comment) : IRequest<ReviewDto>;