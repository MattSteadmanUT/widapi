using System.Linq.Expressions;
using System.Reflection;

namespace NationalWid.Api.Services;

internal readonly record struct SortTerm(string Field, bool Descending);

public static class QuerySorting
{
    public static IOrderedQueryable<T> Apply<T>(IQueryable<T> query, string? sort, params string[] defaultOrder)
    {
        var terms = ParseSort(sort, typeof(T));
        if (terms.Count == 0)
        {
            terms = [.. defaultOrder.Select(field => new SortTerm(field, false))];
        }

        IOrderedQueryable<T>? ordered = null;
        foreach (var term in terms)
        {
            ordered = ApplyTerm(ordered ?? query, term, isFirst: ordered is null);
        }

        return ordered ?? throw new InvalidOperationException("No ordering terms were applied.");
    }

    private static List<SortTerm> ParseSort(string? sort, Type modelType)
    {
        var tokens = QueryFiltering.SplitCsv(sort);
        if (tokens.Count == 0)
            return [];

        var propertyMap = BuildPropertyMap(modelType);
        var terms = new List<SortTerm>(tokens.Count);

        foreach (var token in tokens)
        {
            var parts = token.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length is < 1 or > 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new BadHttpRequestException($"Invalid sort token '{token}'. Use 'field' or 'field:asc|desc'.");
            }

            var field = parts[0];
            if (!propertyMap.TryGetValue(field, out var propertyName)
                && !propertyMap.TryGetValue(NormalizeFieldToken(field), out propertyName))
            {
                throw new BadHttpRequestException($"Unknown sort field '{field}'.");
            }

            var descending = false;
            if (parts.Length == 2)
            {
                var direction = parts[1];
                if (direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
                    descending = false;
                else if (direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                    descending = true;
                else
                    throw new BadHttpRequestException($"Unknown sort direction '{direction}' for field '{field}'.");
            }

            terms.Add(new SortTerm(propertyName, descending));
        }

        return terms;
    }

    private static Dictionary<string, string> BuildPropertyMap(Type modelType)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            map[property.Name] = property.Name;

            var camel = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            map[camel] = property.Name;
            map[NormalizeFieldToken(property.Name)] = property.Name;
            map[NormalizeFieldToken(camel)] = property.Name;
        }

        return map;
    }

    private static string NormalizeFieldToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        Span<char> buffer = stackalloc char[raw.Length];
        var i = 0;
        foreach (var ch in raw)
        {
            if (char.IsLetterOrDigit(ch))
                buffer[i++] = char.ToLowerInvariant(ch);
        }

        return new string(buffer[..i]);
    }

    private static IOrderedQueryable<T> ApplyTerm<T>(IQueryable<T> query, SortTerm term, bool isFirst)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.PropertyOrField(parameter, term.Field);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = isFirst
            ? (term.Descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
            : (term.Descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == methodName
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        var result = method.Invoke(null, [query, lambda]);
        return (IOrderedQueryable<T>)result!;
    }
}
