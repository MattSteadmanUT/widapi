namespace NationalWid.Api.Models;

public sealed record AreaTypeReference
{
    public string StFips { get; init; } = "";

    public string AreaType { get; init; } = "";

    public string AreaTypeName { get; init; } = "";
}

public sealed record StateFipsReference
{
    public string StFips { get; init; } = "";

    public string StateName { get; init; } = "";

    public string StateAbbrev { get; init; } = "";
}
