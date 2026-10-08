using System.Linq.Expressions;

namespace Application.Common.Specifications;

internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body)!;

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}