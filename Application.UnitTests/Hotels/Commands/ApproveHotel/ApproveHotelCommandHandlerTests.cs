using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Commands.ApproveHotel;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.ApproveHotel;

public class ApproveHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IEmailService> _email = new();

    private ApproveHotelCommandHandler CreateHandler() => new(_hotels.Object, _users.Object, _uow.Object, _email.Object);

    [Fact]
    public async Task Handle_PendingHotel_Approves()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid()); // Pending
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Approved);
        _hotels.Verify(r => r.Update(hotel), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PendingHotel_EmailsOwnerWithEncodedHotelName()
    {
        var owner = User.Create("owner@example.com", "hash", "Olivia", "Owner", UserRole.HotelOwner);
        var hotel = Hotel.CreateByOwner("<b>Grand</b>", 5, "d", "addr", 1.0, 2.0, 1, owner.Id);
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _users.Setup(r => r.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>())).ReturnsAsync(owner);

        await CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        _email.Verify(e => e.SendAsync(
            It.Is<EmailMessage>(m => m.ToAddress == "owner@example.com"
                                     && m.HtmlBody.Contains("&lt;b&gt;Grand&lt;/b&gt;")
                                     && !m.HtmlBody.Contains("<b>Grand")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OwnerMissing_ApprovesWithoutEmail()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid());
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Approved);
        _email.Verify(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
