using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AmenityRepository(AppDbContext context) : IAmenityRepository
{
    public async Task<Amenity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task AddAsync(Amenity entity, CancellationToken cancellationToken = default) =>
        await context.Amenities.AddAsync(entity, cancellationToken);

    public void Update(Amenity entity) => context.Amenities.Update(entity);

    public void Remove(Amenity entity) => context.Amenities.Remove(entity);

    public async Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Amenities.AsNoTracking().OrderBy(a => a.Name).ToListAsync(cancellationToken);

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await context.Amenities.AnyAsync(a => a.Name == name && (excludeId == null || a.Id != excludeId),
            cancellationToken);

    public async Task<bool> IsInUseAsync(int amenityId, CancellationToken cancellationToken = default) =>
        await context.HotelAmenities.AnyAsync(ha => ha.AmenityId == amenityId, cancellationToken);

    public async Task<bool> AllExistAsync(IEnumerable<int> amenityIds, CancellationToken cancellationToken = default)
    {
        var ids = amenityIds.Distinct().ToList();
        var found = await context.Amenities.CountAsync(a => ids.Contains(a.Id), cancellationToken);
        return found == ids.Count;
    }
}