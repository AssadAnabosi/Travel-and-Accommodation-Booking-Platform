using Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Turns an <see cref="ISpecification{T}"/> into an <see cref="IQueryable{T}"/> by applying its
/// criteria, includes, ordering and (optionally) paging. Paging can be deferred so a caller may
/// layer extra <c>Where</c> clauses on top and still get an accurate total count before Skip/Take.
/// </summary>
public static class SpecificationEvaluator<TEntity> where TEntity : class
{
    public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification,
        bool evaluatePaging = true)
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (evaluatePaging && specification.IsPagingEnabled)
            query = query.Skip(specification.Skip).Take(specification.Take);

        return query;
    }
}