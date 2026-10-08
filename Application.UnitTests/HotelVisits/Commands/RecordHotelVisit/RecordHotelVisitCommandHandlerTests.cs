using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.HotelVisits.Commands.RecordHotelVisit;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.HotelVisits.Commands.RecordHotelVisit;

public class RecordHotelVisitCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IHotelVisitRepository> _visits = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private RecordHotelVisitCommandHandler CreateHandler() =>
        new(_hotels.Object, _visits.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_LoggedInUser_RecordsVisitWithUserId()
    {
        var userId = Guid.NewGuid();
        _hotels.Setup(h => h.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Hotel(Guid.NewGuid()));
        _currentUser.Setup(c => c.UserId).Returns(userId);
        HotelVisit? recorded = null;
        _visits.Setup(v => v.RecordAsync(It.IsAny<HotelVisit>(), It.IsAny<CancellationToken>()))
            .Callback<HotelVisit, CancellationToken>((v, _) => recorded = v);

        await CreateHandler().Handle(new RecordHotelVisitCommand(1), CancellationToken.None);

        recorded.Should().NotBeNull();
        recorded!.HotelId.Should().Be(1);
        recorded.UserId.Should().Be(userId);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AnonymousCaller_RecordsVisitWithNullUserId()
    {
        // Anonymous views still count toward trending cities.
        _hotels.Setup(h => h.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Hotel(Guid.NewGuid()));
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);
        HotelVisit? recorded = null;
        _visits.Setup(v => v.RecordAsync(It.IsAny<HotelVisit>(), It.IsAny<CancellationToken>()))
            .Callback<HotelVisit, CancellationToken>((v, _) => recorded = v);

        await CreateHandler().Handle(new RecordHotelVisitCommand(1), CancellationToken.None);

        recorded.Should().NotBeNull();
        recorded!.UserId.Should().BeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFoundAndRecordsNothing()
    {
        _hotels.Setup(h => h.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new RecordHotelVisitCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _visits.Verify(v => v.RecordAsync(It.IsAny<HotelVisit>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
