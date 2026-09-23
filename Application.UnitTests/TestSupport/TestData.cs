using System.Collections;
using System.Reflection;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.UnitTests.TestSupport;

/// <summary>
/// Domain entities keep Ids, navigations and child collections private (EF Core populates them).
/// Query handlers read that loaded graph, so tests assemble it the same way EF would: via reflection.
/// </summary>
internal static class EntityReflection
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static T WithId<T>(this T entity, object id) where T : class => entity.With("Id", id);

    public static T With<T>(this T entity, string property, object? value) where T : class
    {
        entity.GetType().GetProperty(property, Instance)!.SetValue(entity, value);
        return entity;
    }

    /// <summary>Appends to a private backing list, e.g. <c>hotel.WithItems("_reviews", review)</c>.</summary>
    public static T WithItems<T>(this T entity, string field, params object[] items) where T : class
    {
        var list = (IList)entity.GetType().GetField(field, Instance)!.GetValue(entity)!;
        foreach (var item in items) list.Add(item);
        return entity;
    }
}

/// <summary>Small builders for the loaded graphs the handlers expect (hotel → rooms → discounts, …).</summary>
internal static class TestData
{
    public static Hotel Hotel(Guid ownerId, bool approved = true, string name = "Grand", int id = 1,
        string cityName = "Paris")
    {
        var hotel = approved
            ? Domain.Entities.Hotel.CreateByAdmin(name, 4, "A nice place", "1 Main St", 1.0, 2.0, 1, ownerId)
            : Domain.Entities.Hotel.CreateByOwner(name, 4, "A nice place", "1 Main St", 1.0, 2.0, 1, ownerId);
        return hotel.WithId(id)
            .With(nameof(Domain.Entities.Hotel.City), City.Create(cityName, "France", "75000"))
            .With(nameof(Domain.Entities.Hotel.Owner), User.Create("owner@tabp.dev", "hash", "Olivia", "Owner"));
    }

    /// <summary>Creates a room, links it to <paramref name="hotel"/> both ways, and returns it.</summary>
    public static Room RoomIn(Hotel hotel, decimal basePrice = 100m, string number = "101", int id = 10,
        RoomType type = RoomType.Standard)
    {
        var room = Room.Create(hotel.Id, number, type, 2, 1, Money.Of(basePrice))
            .WithId(id)
            .With(nameof(Room.Hotel), hotel);
        hotel.WithItems("_rooms", room);
        return room;
    }

    /// <summary>Creates a discount on <paramref name="room"/> (both ways) valid from/to the given dates.</summary>
    public static Discount DiscountOn(Room room, DiscountType type, decimal value, DateOnly from, DateOnly to,
        int id = 100)
    {
        var discount = Discount.Create(room.Id, "Deal", type, value, from, to)
            .WithId(id)
            .With(nameof(Discount.Room), room);
        room.WithItems("_discounts", discount);
        return discount;
    }

    public static Booking BookingFor(User guest, Room room, bool confirmed = false)
    {
        var booking = Booking.Create(guest.Id, room.Id,
                DateRange.Of(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 4)), 2, 1, Money.Of(300m), "Quiet room")
            .With(nameof(Booking.User), guest)
            .With(nameof(Booking.Room), room);
        if (confirmed) booking.Confirm();
        return booking;
    }
}
