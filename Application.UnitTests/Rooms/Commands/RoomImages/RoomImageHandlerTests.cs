using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.AddRoomImage;
using Application.Features.Rooms.Commands.RemoveRoomImage;
using Application.Features.Rooms.Queries.GetRoomImages;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.RoomImages;

public class RoomImageHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private Room GivenRoom()
    {
        var room = TestData.RoomIn(TestData.Hotel(_ownerId));
        _rooms.Setup(r => r.GetByIdWithImagesTrackedAsync(room.Id, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        return room;
    }

    private void SignedInAsOwner(Guid ownerId)
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
    }

    [Fact]
    public async Task Add_Owner_AddsImageAndSaves()
    {
        var room = GivenRoom();
        SignedInAsOwner(_ownerId);

        await new AddRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new AddRoomImageCommand(room.Id, "https://img.example/r.jpg"), CancellationToken.None);

        room.Images.Should().ContainSingle(i => i.Url == "https://img.example/r.jpg");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_OtherOwner_ThrowsForbidden()
    {
        var room = GivenRoom();
        SignedInAsOwner(Guid.NewGuid());

        var act = () => new AddRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new AddRoomImageCommand(room.Id, "https://img.example/r.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        room.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_UnknownRoom_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var act = () => new AddRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new AddRoomImageCommand(99, "https://img.example/r.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Remove_Admin_RemovesImageAndSaves()
    {
        var room = GivenRoom();
        room.AddImage("https://img.example/r.jpg").WithId(5);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        await new RemoveRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new RemoveRoomImageCommand(room.Id, 5), CancellationToken.None);

        room.Images.Should().BeEmpty();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_ImageNotOnThisRoom_ThrowsImageNotFound()
    {
        var room = GivenRoom();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var act = () => new RemoveRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new RemoveRoomImageCommand(room.Id, 12345), CancellationToken.None);

        await act.Should().ThrowAsync<ImageNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Remove_OtherOwner_ThrowsForbidden()
    {
        var room = GivenRoom();
        room.AddImage("https://img.example/r.jpg").WithId(5);
        SignedInAsOwner(Guid.NewGuid());

        var act = () => new RemoveRoomImageCommandHandler(_rooms.Object, _uow.Object, _currentUser.Object)
            .Handle(new RemoveRoomImageCommand(room.Id, 5), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        room.Images.Should().ContainSingle();
    }

    [Fact]
    public async Task List_Owner_ReturnsIdsInDisplayOrder()
    {
        var room = GivenRoom();
        room.AddImage("https://img.example/1.jpg").WithId(7);
        room.AddImage("https://img.example/2.jpg").WithId(3);
        SignedInAsOwner(_ownerId);

        var images = await new GetRoomImagesQueryHandler(_rooms.Object, _currentUser.Object)
            .Handle(new GetRoomImagesQuery(room.Id), CancellationToken.None);

        images.Select(i => (i.Id, i.Url)).Should().Equal((7, "https://img.example/1.jpg"), (3, "https://img.example/2.jpg"));
    }

    [Fact]
    public async Task List_OtherOwner_ThrowsForbidden()
    {
        var room = GivenRoom();
        SignedInAsOwner(Guid.NewGuid());

        var act = () => new GetRoomImagesQueryHandler(_rooms.Object, _currentUser.Object)
            .Handle(new GetRoomImagesQuery(room.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
