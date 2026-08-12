using NationalWid.Api.Models;

namespace NationalWid.Api.Services;

public static class TableCatalog
{
    public static readonly IReadOnlyList<TableCatalogEntry> Entries =
    [
        new() { TableName = "LaborForce", TableClass = "core", Endpoint = "/labor-force", Implemented = true },
        new() { TableName = "CES", TableClass = "core", Endpoint = "/ces", Implemented = true },
        new() { TableName = "Industry", TableClass = "core", Endpoint = "/industry", Implemented = true },
        new() { TableName = "IOWage", TableClass = "core", Endpoint = "/wages", Implemented = true },
        new() { TableName = "ProjectionsMatrix", TableClass = "core", Endpoint = "/projections", Implemented = true },
        new() { TableName = "License", TableClass = "core", Endpoint = "/licensing/licenses", Implemented = true },
        new() { TableName = "LicenseAuthorities", TableClass = "non-core", Endpoint = "/licensing/authorities", Implemented = true },
        new() { TableName = "LicenseHistory", TableClass = "non-core", Endpoint = "/licensing/history", Implemented = true },
        new() { TableName = "LicenseXOcc", TableClass = "non-core", Endpoint = "/licensing/occupationCrosswalks", Implemented = true },

        new() { TableName = "Geographies", TableClass = "non-core", Endpoint = "/lookups/geographies", Implemented = true },
        new() { TableName = "AreaTypes", TableClass = "non-core", Endpoint = "/lookups/areaTypes", Implemented = true },
        new() { TableName = "StateFips", TableClass = "non-core", Endpoint = "/lookups/stateFips", Implemented = true },
        new() { TableName = "PeriodTypes", TableClass = "non-core", Endpoint = "/lookups/periodTypes", Implemented = true },
        new() { TableName = "PeriodYears", TableClass = "non-core", Endpoint = "/lookups/periodYears", Implemented = true },
        new() { TableName = "Periods", TableClass = "non-core", Endpoint = "/lookups/periods", Implemented = true },
        new() { TableName = "IndustryCodes", TableClass = "non-core", Endpoint = "/lookups/industryCodes", Implemented = true },
        new() { TableName = "OccupationCodes", TableClass = "non-core", Endpoint = "/lookups/occupationCodes", Implemented = true },
        new() { TableName = "CESCodes", TableClass = "non-core", Endpoint = "/lookups/cesCodes", Implemented = true },
        new() { TableName = "Ownerships", TableClass = "non-core", Endpoint = "/lookups/ownerships", Implemented = true },
        new() { TableName = "WageSources", TableClass = "non-core", Endpoint = "/lookups/wageSources", Implemented = true },
        new() { TableName = "WageRateTypes", TableClass = "non-core", Endpoint = "/lookups/wageRateTypes", Implemented = true },
        new() { TableName = "Benchmark", TableClass = "non-core", Endpoint = "/lookups/benchmarks", Implemented = true },
        new() { TableName = "GrowthCodes", TableClass = "non-core", Endpoint = "/lookups/growthCodes", Implemented = true },
        new() { TableName = "IndDirectories", TableClass = "non-core", Endpoint = "/lookups/ind-directories", Implemented = true },
        new() { TableName = "OccDirectories", TableClass = "non-core", Endpoint = "/lookups/occ-directories", Implemented = true },
        new() { TableName = "MatrixXInd", TableClass = "non-core", Endpoint = "/projections/matrixXInd", Implemented = true },
        new() { TableName = "MatrixXOcc", TableClass = "non-core", Endpoint = "/projections/matrixXOcc", Implemented = true },

        new() { TableName = "CesWithGeography", TableClass = "non-core", Endpoint = "/views/cesWithGeography", Implemented = true },
        new() { TableName = "LaborForceWithGeography", TableClass = "non-core", Endpoint = "/views/laborForceWithGeography", Implemented = true },
        new() { TableName = "WageWithDescriptions", TableClass = "non-core", Endpoint = "/views/wagesWithDescriptions", Implemented = true },
        new() { TableName = "ProjectionWithTitles", TableClass = "non-core", Endpoint = "/views/projectionsWithTitles", Implemented = true },
        new() { TableName = "IndustryWithGeography", TableClass = "non-core", Endpoint = "/views/industryWithGeography", Implemented = true },
        new() { TableName = "LicensingByOccupation", TableClass = "non-core", Endpoint = "/views/licensingByOccupation", Implemented = true },

        new() { TableName = "CPI", TableClass = "non-core", Endpoint = "/cpi", Implemented = true },
        new() { TableName = "CpiSeries", TableClass = "non-core", Endpoint = "/cpi/metadata", Implemented = true },
        new() { TableName = "CpiItems", TableClass = "non-core", Endpoint = "/cpi/items", Implemented = true },
        new() { TableName = "CpiAreas", TableClass = "non-core", Endpoint = "/cpi/areas", Implemented = true },
    ];
}