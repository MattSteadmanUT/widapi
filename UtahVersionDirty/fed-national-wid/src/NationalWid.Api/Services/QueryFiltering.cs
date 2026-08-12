using System.Linq.Expressions;
using System.Reflection;

namespace NationalWid.Api.Services;

internal enum FilterMatchMode
{
    Exact,
    StartsWith,
    EndsWith,
    Contains,
}

internal readonly record struct FilterTerm(string Value, FilterMatchMode Mode);

public static class QueryFiltering
{
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(
        nameof(string.StartsWith),
        [typeof(string)])
        ?? throw new InvalidOperationException("Could not resolve string.StartsWith.");

    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(
        nameof(string.EndsWith),
        [typeof(string)])
        ?? throw new InvalidOperationException("Could not resolve string.EndsWith.");

    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(
        nameof(string.Contains),
        [typeof(string)])
        ?? throw new InvalidOperationException("Could not resolve string.Contains.");

    public static IReadOnlyList<string> SplitCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    public static IQueryable<T> ApplyStringFilter<T>(
        IQueryable<T> query,
        string propertyName,
        string? raw,
        Func<string, string?>? normalizer = null,
        bool allowWildcard = false)
    {
        var terms = ParseTerms(raw, normalizer, allowWildcard);
        if (terms.Count == 0)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, propertyName);
        Expression? predicate = null;

        foreach (var term in terms)
        {
            Expression clause = term.Mode switch
            {
                FilterMatchMode.Exact => Expression.Equal(
                    property,
                    Expression.Constant(term.Value, property.Type)),
                FilterMatchMode.StartsWith => BuildStringCall(property, StartsWithMethod, term.Value),
                FilterMatchMode.EndsWith => BuildStringCall(property, EndsWithMethod, term.Value),
                FilterMatchMode.Contains => BuildStringCall(property, ContainsMethod, term.Value),
                _ => throw new InvalidOperationException("Unknown filter match mode."),
            };

            predicate = predicate is null ? clause : Expression.OrElse(predicate, clause);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(predicate!, parameter);
        return query.Where(lambda);
    }

    private static IReadOnlyList<FilterTerm> ParseTerms(
        string? raw,
        Func<string, string?>? normalizer,
        bool allowWildcard)
    {
        var values = SplitCsv(raw);
        if (values.Count == 0)
            return [];

        var terms = new List<FilterTerm>(values.Count);
        foreach (var value in values)
        {
            var normalized = normalizer?.Invoke(value) ?? value;
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            if (allowWildcard && normalized.Contains('*'))
            {
                terms.Add(ParseWildcardTerm(normalized));
                continue;
            }

            terms.Add(new FilterTerm(normalized, FilterMatchMode.Exact));
        }

        return terms;
    }

    private static FilterTerm ParseWildcardTerm(string pattern)
    {
        var startsWithWildcard = pattern.StartsWith('*');
        var endsWithWildcard = pattern.EndsWith('*');
        var token = pattern.Replace("*", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrEmpty(token))
            return new FilterTerm(string.Empty, FilterMatchMode.Contains);

        if (startsWithWildcard && endsWithWildcard)
            return new FilterTerm(token, FilterMatchMode.Contains);

        if (startsWithWildcard)
            return new FilterTerm(token, FilterMatchMode.EndsWith);

        if (endsWithWildcard)
            return new FilterTerm(token, FilterMatchMode.StartsWith);

        return new FilterTerm(token, FilterMatchMode.Contains);
    }

    private static Expression BuildStringCall(Expression property, MethodInfo method, string value)
    {
        var notNull = Expression.NotEqual(property, Expression.Constant(null, property.Type));
        var call = Expression.Call(property, method, Expression.Constant(value));

        return Expression.AndAlso(notNull, call);
    }
}
