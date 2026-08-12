namespace NationalWid.Api.Services;

public static class LookupCatalog
{
    public static readonly IReadOnlyDictionary<string, string> AreaTypeTitles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["00"] = "National",
            ["01"] = "State",
            ["04"] = "County",
            ["21"] = "MSA",
            ["31"] = "Metropolitan Statistical Area",
        };

    public static readonly IReadOnlyDictionary<string, (string Name, string Abbrev)> StateFips =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["00"] = ("United States", "US"),
            ["01"] = ("Alabama", "AL"),
            ["02"] = ("Alaska", "AK"),
            ["04"] = ("Arizona", "AZ"),
            ["05"] = ("Arkansas", "AR"),
            ["06"] = ("California", "CA"),
            ["08"] = ("Colorado", "CO"),
            ["09"] = ("Connecticut", "CT"),
            ["10"] = ("Delaware", "DE"),
            ["11"] = ("District of Columbia", "DC"),
            ["12"] = ("Florida", "FL"),
            ["13"] = ("Georgia", "GA"),
            ["15"] = ("Hawaii", "HI"),
            ["16"] = ("Idaho", "ID"),
            ["17"] = ("Illinois", "IL"),
            ["18"] = ("Indiana", "IN"),
            ["19"] = ("Iowa", "IA"),
            ["20"] = ("Kansas", "KS"),
            ["21"] = ("Kentucky", "KY"),
            ["22"] = ("Louisiana", "LA"),
            ["23"] = ("Maine", "ME"),
            ["24"] = ("Maryland", "MD"),
            ["25"] = ("Massachusetts", "MA"),
            ["26"] = ("Michigan", "MI"),
            ["27"] = ("Minnesota", "MN"),
            ["28"] = ("Mississippi", "MS"),
            ["29"] = ("Missouri", "MO"),
            ["30"] = ("Montana", "MT"),
            ["31"] = ("Nebraska", "NE"),
            ["32"] = ("Nevada", "NV"),
            ["33"] = ("New Hampshire", "NH"),
            ["34"] = ("New Jersey", "NJ"),
            ["35"] = ("New Mexico", "NM"),
            ["36"] = ("New York", "NY"),
            ["37"] = ("North Carolina", "NC"),
            ["38"] = ("North Dakota", "ND"),
            ["39"] = ("Ohio", "OH"),
            ["40"] = ("Oklahoma", "OK"),
            ["41"] = ("Oregon", "OR"),
            ["42"] = ("Pennsylvania", "PA"),
            ["44"] = ("Rhode Island", "RI"),
            ["45"] = ("South Carolina", "SC"),
            ["46"] = ("South Dakota", "SD"),
            ["47"] = ("Tennessee", "TN"),
            ["48"] = ("Texas", "TX"),
            ["49"] = ("Utah", "UT"),
            ["50"] = ("Vermont", "VT"),
            ["51"] = ("Virginia", "VA"),
            ["53"] = ("Washington", "WA"),
            ["54"] = ("West Virginia", "WV"),
            ["55"] = ("Wisconsin", "WI"),
            ["56"] = ("Wyoming", "WY"),
            ["60"] = ("American Samoa", "AS"),
            ["66"] = ("Guam", "GU"),
            ["69"] = ("Northern Mariana Islands", "MP"),
            ["72"] = ("Puerto Rico", "PR"),
            ["78"] = ("U.S. Virgin Islands", "VI"),
        };

    public static readonly IReadOnlyDictionary<string, string> PeriodTypeTitles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["01"] = "Annual",
            ["03"] = "Monthly",
            ["04"] = "Quarterly",
        };

    public static readonly IReadOnlyDictionary<string, string> OwnershipTitles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["0"] = "Total",
            ["1"] = "Federal Government",
            ["2"] = "State Government",
            ["3"] = "Local Government",
            ["5"] = "Private",
            ["10"] = "Total",
            ["20"] = "Federal Government",
            ["30"] = "State Government",
            ["40"] = "Local Government",
            ["50"] = "Private",
        };

    public static readonly IReadOnlyList<(string StFips, string WageSource, string WageSourceTitle)> WageSources =
    [
        ("00", "3", "OES/OEWS"),
    ];

    public static readonly IReadOnlyList<(string RateType, string RateTypeTitle)> WageRateTypes =
    [
        ("1", "Hourly"),
        ("2", "Annual"),
    ];

    public static readonly IReadOnlyList<(string StFips, string GrowthCode, string GrowthCodeTitle)> GrowthCodes =
    [
        ("00", "AA", "Rapid growth"),
        ("00", "BB", "Moderate growth"),
        ("00", "CC", "Stable"),
        ("00", "DD", "Decline"),
    ];
}