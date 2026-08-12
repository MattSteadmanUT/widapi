namespace NationalWid.Api.Models;

public sealed record AreaTypeLookup
{
    public string StFips { get; init; } = "";

    public string AreaType { get; init; } = "";

    public string? AreaTypeName { get; init; }
}

public sealed record StateFipsLookup
{
    public string StFips { get; init; } = "";

    public string StateName { get; init; } = "";

    public string StateAbbrev { get; init; } = "";
}

public sealed record PeriodTypeLookup
{
    public string PeriodType { get; init; } = "";

    public string PeriodTypeName { get; init; } = "";
}

public sealed record PeriodLookup
{
    public string PeriodType { get; init; } = "";

    public string Period { get; init; } = "";
}

public sealed record IndustryCodeLookup
{
    public string StFips { get; init; } = "";

    public string CodeType { get; init; } = "";

    public string Code { get; init; } = "";

    public string? CodeTitle { get; init; }

    public string? CodeTitleLong { get; init; }

    public string? CodeLevel { get; init; }
}

public sealed record OccupationCodeLookup
{
    public string StFips { get; init; } = "";

    public string CodeType { get; init; } = "";

    public string Code { get; init; } = "";

    public string? CodeTitle { get; init; }

    public string? CodeTitleLong { get; init; }

    public string? CodeLevel { get; init; }

    public string? SubTotal { get; init; }
}

public sealed record OwnershipLookup
{
    public string Ownership { get; init; } = "";

    public string? OwnershipTitle { get; init; }
}

public sealed record WageSourceLookup
{
    public string StFips { get; init; } = "";

    public string WageSource { get; init; } = "";

    public string WageSourceTitle { get; init; } = "";
}

public sealed record WageRateTypeLookup
{
    public string RateType { get; init; } = "";

    public string RateTypeTitle { get; init; } = "";
}

public sealed record BenchmarkLookup
{
    public string Benchmark { get; init; } = "";
}

public sealed record GrowthCodeLookup
{
    public string StFips { get; init; } = "";

    public string GrowthCode { get; init; } = "";

    public string GrowthCodeTitle { get; init; } = "";
}
