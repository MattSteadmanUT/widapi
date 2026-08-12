namespace NationalWid.Api.Models;

public sealed record AreaKey
{
    public string StFips { get; init; } = "";

    public string AreaType { get; init; } = "";

    public string AreaTypeVersion { get; init; } = "0";

    public string Area { get; init; } = "";
}

public sealed record PeriodKey
{
    public string PeriodType { get; init; } = "";

    public string Period { get; init; } = "";
}

public sealed record PeriodPoint
{
    public string? PeriodYear { get; init; }

    public string? PeriodType { get; init; }

    public string? Period { get; init; }
}

public sealed record TableMetadata
{
    public IReadOnlyList<AreaKey>? Areas { get; init; }

    public IReadOnlyList<string>? Years { get; init; }

    public IReadOnlyList<PeriodKey>? Periods { get; init; }

    public PeriodPoint? MinPeriod { get; init; }

    public PeriodPoint? MaxPeriod { get; init; }

    public IReadOnlyList<string>? ProjectedYears { get; init; }
}

public sealed record ApiObjectResponse<T>
{
    public required ApiMeta Meta { get; init; }

    public required T Data { get; init; }

    public required ApiLinks Links { get; init; }
}
