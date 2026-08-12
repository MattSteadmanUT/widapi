using System.Globalization;

namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>LicenseAuthority</c> table — licensing and regulatory authority (board/agency) contact information.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, LicAuthID.
/// </summary>
public sealed record LicenseAuthority
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code for the authority's jurisdiction.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0'.</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code for the authority's jurisdiction.</summary>
    public string Area { get; init; } = "";

    /// <summary>Licensing authority identifier.</summary>
    public string LicAuthID { get; init; } = "";

    /// <summary>Department name within the licensing authority.</summary>
    public string? Department { get; init; }

    /// <summary>Division name within the department.</summary>
    public string? Division { get; init; }

    /// <summary>Board or committee name.</summary>
    public string? Board { get; init; }

    /// <summary>Mailing address line 1.</summary>
    public string? Address1 { get; init; }

    /// <summary>City of the authority's mailing address.</summary>
    public string? City { get; init; }

    /// <summary>State abbreviation of the authority's mailing address.</summary>
    public string? State { get; init; }

    /// <summary>ZIP code of the authority's mailing address.</summary>
    public string? ZipCode { get; init; }

    /// <summary>Telephone number.</summary>
    public string? Telephone { get; init; }

    /// <summary>Contact email address.</summary>
    public string? Email { get; init; }

    /// <summary>Website URL for the licensing authority.</summary>
    public string? Url { get; init; }
}

/// <summary>
/// WID 3.0 <c>Licenses</c> table — occupational license definitions and requirements.
/// Primary key: StFips, LicenseID.
/// </summary>
public sealed record License
{
    private static DateOnly? ParseWidDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>License identifier.</summary>
    public string LicenseID { get; init; } = "";

    /// <summary>Licensing authority identifier; foreign key to <see cref="LicenseAuthority"/>.</summary>
    public string? LicAuthID { get; init; }

    /// <summary>Human-readable license title.</summary>
    public string? LicenseTitle { get; init; }

    /// <summary>License type code.</summary>
    public string? LicenseType { get; init; }

    /// <summary>Exam required indicator ('1' = yes, '0' = no).</summary>
    public string Exam { get; init; } = "0";

    /// <summary>Education requirement indicator ('1' = yes, '0' = no).</summary>
    public string Education { get; init; } = "0";

    /// <summary>Continuing education requirement indicator ('1' = yes, '0' = no).</summary>
    public string ContinuingEdu { get; init; } = "0";

    /// <summary>Certification requirement indicator ('1' = yes, '0' = no).</summary>
    public string Certification { get; init; } = "0";

    /// <summary>Experience requirement indicator ('1' = yes, '0' = no).</summary>
    public string Experience { get; init; } = "0";

    /// <summary>Criminal background check requirement indicator ('1' = yes, '0' = no).</summary>
    public string Criminal { get; init; } = "0";

    /// <summary>Physical requirements indicator ('1' = yes, '0' = no).</summary>
    public string PhysicalReq { get; init; } = "0";

    /// <summary>Veteran preference indicator ('1' = yes, '0' = no).</summary>
    public string Veteran { get; init; } = "0";

    /// <summary>Inactive license indicator ('1' = inactive, '0' = active).</summary>
    public string Inactive { get; init; } = "0";

    /// <summary>URL for additional license information.</summary>
    public string? LicenseURL { get; init; }

    /// <summary>Date the license record was last updated (string, format varies by state).</summary>
    public string? LicenseUpdated { get; init; }

    /// <summary>Date the license record was last updated, parsed from the source stamp when available.</summary>
    public DateOnly? LicenseUpdatedDate => ParseWidDate(LicenseUpdated);
}

/// <summary>
/// WID 3.0 <c>LicenseHistory</c> table — historical counts of active licenses by area and period.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, LicenseID.
/// </summary>
public sealed record LicenseHistory
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0'.</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code.</summary>
    public string Area { get; init; } = "";

    /// <summary>Data year.</summary>
    public string PeriodYear { get; init; } = "";

    /// <summary>Period type code.</summary>
    public string PeriodType { get; init; } = "";

    /// <summary>Period code.</summary>
    public string Period { get; init; } = "";

    /// <summary>License identifier; foreign key to <see cref="License"/>.</summary>
    public string LicenseID { get; init; } = "";

    /// <summary>Type of license number reported (e.g. 'Active', 'Total').</summary>
    public string? LicenseNumberType { get; init; }

    /// <summary>Count of licenses of the given type in the period.</summary>
    public int? LicenseNumber { get; init; }
}

/// <summary>
/// WID 3.0 <c>LicenseXOcc</c> crosswalk table — maps each license to the SOC occupation codes it applies to.
/// Primary key: StFips, LicenseID, OccCodeType, OccCode.
/// </summary>
public sealed record LicenseXOcc
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>License identifier; foreign key to <see cref="License"/>.</summary>
    public string LicenseID { get; init; } = "";

    /// <summary>Occupation code type identifier.</summary>
    public string OccCodeType { get; init; } = "";

    /// <summary>Occupation code, trimmed of hyphens.</summary>
    public string OccCode { get; init; } = "";
}
