using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Hotels.Commands.DeleteHotel;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.DeleteHotel;

public class DeleteHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DeleteHotelCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object);

    [Fact]
    public async Task Handle_HotelWithoutRooms_RemovesAndSaves()
    {
        var hotel = TestData.Hotel(Guid.NewGuid(), id: 4);
        _hotels.Setup(h => h.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _hotels.Setup(h => h.HasRoomsAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteHotelCommand(4), CancellationToken.None);

        _hotels.Verify(h => h.Remove(hotel), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HotelWithRooms_ThrowsConflictEvenThoughRoomsAreNotLoaded()
    {
        // Regression: GetByIdAsync doesn't load Rooms, so the old `hotel.Rooms.Count > 0` guard
        // was always false and the Cascade FK silently deleted every room. The check must query the database.
        var hotel = TestData.Hotel(Guid.NewGuid(), id: 4); // Rooms collection empty, as GetByIdAsync returns it
        _hotels.Setup(h => h.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _hotels.Setup(h => h.HasRoomsAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new DeleteHotelCommand(4), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _hotels.Verify(h => h.Remove(It.IsAny<Hotel>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new DeleteHotelCommand(4), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
