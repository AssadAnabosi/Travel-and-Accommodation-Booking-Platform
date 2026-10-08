using System.Linq.Expressions;

namespace Application.Common.Specifications;

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Skip { get; private set; }
    public int Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    /// <summary>
    /// Lets a concrete spec build its filter one optional field at a time (e.g. "if a keyword was given, AND this in")
    /// instead of one giant hand-written boolean expression per query method.
    /// </summary>
    protected void AddCriteria(Expression<Func<T, bool>> criteria) =>
        Criteria = Criteria is null ? criteria : Criteria.AndAlso(criteria);

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
    protected void ApplyOrderBy(Expression<Func<T, object>> expression) => OrderBy = expression;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> expression) => OrderByDescending = expression;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}