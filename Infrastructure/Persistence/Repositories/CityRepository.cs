using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CityRepository(AppDbContext context) : ICityRepository
{
    public async Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(City entity, CancellationToken cancellationToken = default) =>
        await context.Cities.AddAsync(entity, cancellationToken);

    public void Update(City entity) => context.Cities.Update(entity);

    public void Remove(City entity) => context.Cities.Remove(entity);

    public async Task<PaginatedList<City>> SearchAsync(string? nameFilter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Cities.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(c => c.Name.Contains(nameFilter));

        query = query.OrderBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<City>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await context.Cities.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), cancellationToken);

    public async Task<bool> HasHotelsAsync(int cityId, CancellationToken cancellationToken = default) =>
        await context.Hotels.AnyAsync(h => h.CityId == cityId, cancellationToken);
}