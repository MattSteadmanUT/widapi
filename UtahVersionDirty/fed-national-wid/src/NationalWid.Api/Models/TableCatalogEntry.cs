namespace NationalWid.Api.Models;

public sealed record TableCatalogEntry
{
    public string TableName { get; init; } = "";

    public string TableClass { get; init; } = "";

    public string Endpoint { get; init; } = "";

    public bool Implemented { get; init; }
}