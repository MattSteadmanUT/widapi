using System.Text.Json.Serialization;

namespace NationalWid.Ingestion.Models;

// Wire models for the BLS Public Data API v2 timeseries endpoint.

public sealed class BlsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("message")]
    public List<string> Message { get; set; } = [];

    [JsonPropertyName("Results")]
    public BlsResults? Results { get; set; }
}

public sealed class BlsResults
{
    [JsonPropertyName("series")]
    public List<BlsSeries> Series { get; set; } = [];
}

public sealed class BlsSeries
{
    [JsonPropertyName("seriesID")]
    public string SeriesId { get; set; } = "";

    [JsonPropertyName("data")]
    public List<BlsDataPoint> Data { get; set; } = [];
}

public sealed class BlsDataPoint
{
    [JsonPropertyName("year")]
    public string Year { get; set; } = "";

    /// <summary>BLS period code: M01–M12 (months), M13 (annual average), Q01–Q04, A01.</summary>
    [JsonPropertyName("period")]
    public string Period { get; set; } = "";

    [JsonPropertyName("periodName")]
    public string PeriodName { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("footnotes")]
    public List<BlsFootnote> Footnotes { get; set; } = [];
}

public sealed class BlsFootnote
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>One row of the QCEW open-data API (data.bls.gov/cew/data/api).</summary>
public sealed class QcewRecord
{
    [JsonPropertyName("area_fips")]
    public string AreaFips { get; set; } = "";

    [JsonPropertyName("own_code")]
    public string OwnCode { get; set; } = "";

    [JsonPropertyName("industry_code")]
    public string IndustryCode { get; set; } = "";

    [JsonPropertyName("agglvl_code")]
    public string AggLvlCode { get; set; } = "";

    [JsonPropertyName("year")]
    public string Year { get; set; } = "";

    [JsonPropertyName("qtr")]
    public string Qtr { get; set; } = "";

    [JsonPropertyName("disclosure_code")]
    public string? DisclosureCode { get; set; }

    [JsonPropertyName("qtrly_estabs")]
    public long? QtrlyEstabs { get; set; }

    [JsonPropertyName("month1_emplvl")]
    public long? Month1Emplvl { get; set; }

    [JsonPropertyName("month2_emplvl")]
    public long? Month2Emplvl { get; set; }

    [JsonPropertyName("month3_emplvl")]
    public long? Month3Emplvl { get; set; }

    [JsonPropertyName("total_qtrly_wages")]
    public decimal? TotalQtrlyWages { get; set; }

    [JsonPropertyName("taxable_qtrly_wages")]
    public decimal? TaxableQtrlyWages { get; set; }

    [JsonPropertyName("qtrly_contributions")]
    public decimal? QtrlyContributions { get; set; }

    [JsonPropertyName("avg_wkly_wage")]
    public decimal? AvgWklyWage { get; set; }
}
