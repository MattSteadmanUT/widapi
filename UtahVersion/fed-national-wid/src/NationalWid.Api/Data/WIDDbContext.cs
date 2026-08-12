using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Models;

namespace NationalWid.Api.Data;

/// <summary>
/// EF Core context over the WID 3.0 PostgreSQL schema.
/// Tables and columns are all-lowercase (see cloud-deployment/migrations).
/// </summary>
public sealed class WIDDbContext(DbContextOptions<WIDDbContext> options) : DbContext(options)
{
    public DbSet<LaborForce> LaborForce => Set<LaborForce>();

    public DbSet<Ces> Ces => Set<Ces>();

    public DbSet<Industry> Industry => Set<Industry>();

    public DbSet<IOWage> IOWages => Set<IOWage>();

    public DbSet<ProjectionsMatrix> ProjectionsMatrix => Set<ProjectionsMatrix>();

    public DbSet<Geography> Geographies => Set<Geography>();

    public DbSet<AreaTypeReference> AreaTypeReferences => Set<AreaTypeReference>();

    public DbSet<StateFipsReference> StateFipsReferences => Set<StateFipsReference>();

    public DbSet<PeriodYear> PeriodYears => Set<PeriodYear>();

    public DbSet<CesCode> CesCodes => Set<CesCode>();

    public DbSet<IndDirectory> IndDirectories => Set<IndDirectory>();

    public DbSet<OccDirectory> OccDirectories => Set<OccDirectory>();

    public DbSet<MatrixXInd> MatrixXInd => Set<MatrixXInd>();

    public DbSet<MatrixXOcc> MatrixXOcc => Set<MatrixXOcc>();

    public DbSet<LicenseAuthority> LicenseAuthorities => Set<LicenseAuthority>();

    public DbSet<License> Licenses => Set<License>();

    public DbSet<LicenseHistory> LicenseHistory => Set<LicenseHistory>();

    public DbSet<LicenseXOcc> LicenseXOcc => Set<LicenseXOcc>();

    public DbSet<IngestLog> IngestLogs => Set<IngestLog>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<Cpi> Cpi => Set<Cpi>();
    public DbSet<CpiSeries> CpiSeries => Set<CpiSeries>();
    public DbSet<CpiItem> CpiItems => Set<CpiItem>();
    public DbSet<CpiArea> CpiAreas => Set<CpiArea>();
    public DbSet<CpiPeriodicity> CpiPeriodicities => Set<CpiPeriodicity>();
    public DbSet<CpiSeasonalAdjustment> CpiSeasonalAdjustments => Set<CpiSeasonalAdjustment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LaborForce>(e =>
        {
            e.ToTable("laborforce");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.PeriodYear, x.PeriodType, x.Period, x.Adjusted });
            e.Property(x => x.CivilianLaborForce).HasColumnName("laborforce");
            // DB column has an extra 'r' (WID spec typo preserved for backwards compatibility)
            e.Property(x => x.EmpPopRatio).HasColumnName("emppoproatio");
        });

        modelBuilder.Entity<Ces>(e =>
        {
            e.ToTable("ces");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.PeriodYear, x.PeriodType, x.Period, x.SeriesCodeType, x.SeriesCode, x.Adjusted });
        });

        modelBuilder.Entity<Industry>(e =>
        {
            e.ToTable("industry");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.PeriodYear, x.PeriodType, x.Period, x.Ownership, x.IndCodeType, x.IndCode });
        });

        modelBuilder.Entity<IOWage>(e =>
        {
            e.ToTable("iowage");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.PeriodYear, x.PeriodType, x.Period, x.IndCodeType, x.IndCode, x.OccCodeType, x.OccCode });
            // DB column has a typo ('ranage' instead of 'range') preserved for backwards compatibility
            e.Property(x => x.UserDefinedRangeMean).HasColumnName("userdefinedranagemean");
        });

        modelBuilder.Entity<ProjectionsMatrix>(e =>
        {
            e.ToTable("projectionsmatrix");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.ProjectionsPeriod, x.IndCodeType, x.IndCode, x.OccCodeType, x.OccCode });
        });

        modelBuilder.Entity<Geography>(e =>
        {
            e.ToTable("geographies");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area });
        });

        modelBuilder.Entity<AreaTypeReference>(e =>
        {
            e.ToTable("areatypes");
            e.HasKey(x => new { x.StFips, x.AreaType });
        });

        modelBuilder.Entity<StateFipsReference>(e =>
        {
            e.ToTable("statefips");
            e.HasKey(x => x.StFips);
        });

        modelBuilder.Entity<PeriodYear>(e =>
        {
            e.ToTable("periodyears");
            e.HasKey(x => new { x.StFips, x.Year, x.PeriodType, x.Period });
            e.Property(x => x.Year).HasColumnName("periodyear");
        });

        modelBuilder.Entity<CesCode>(e =>
        {
            e.ToTable("cescodes");
            e.HasKey(x => new { x.StFips, x.SeriesCodeType, x.SeriesCode });
        });

        modelBuilder.Entity<IndDirectory>(e =>
        {
            e.ToTable("inddirectories");
            e.HasKey(x => new { x.StFips, x.ProjPeriod, x.IndCodeType, x.IndCode });
        });

        modelBuilder.Entity<OccDirectory>(e =>
        {
            e.ToTable("occdirectories");
            e.HasKey(x => new { x.StFips, x.ProjPeriod, x.OccCodeType, x.OccCode });
        });

        modelBuilder.Entity<MatrixXInd>(e =>
        {
            e.ToTable("matrixxind");
            e.HasKey(x => new { x.StFips, x.MatrixIndCode, x.IndCodeType, x.IndCode });
        });

        modelBuilder.Entity<MatrixXOcc>(e =>
        {
            e.ToTable("matrixxocc");
            e.HasKey(x => new { x.StFips, x.MatrixOccCode, x.OccCodeType, x.OccCode });
        });

        modelBuilder.Entity<LicenseAuthority>(e =>
        {
            e.ToTable("licenseauthorities");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.LicAuthID });
        });

        modelBuilder.Entity<License>(e =>
        {
            e.ToTable("license");
            e.HasKey(x => new { x.StFips, x.LicenseID });
        });

        modelBuilder.Entity<LicenseHistory>(e =>
        {
            e.ToTable("licensehistory");
            e.HasKey(x => new { x.StFips, x.AreaType, x.AreaTypeVersion, x.Area, x.PeriodYear, x.PeriodType, x.Period, x.LicenseID });
        });

        modelBuilder.Entity<LicenseXOcc>(e =>
        {
            e.ToTable("licensexocc");
            e.HasKey(x => new { x.StFips, x.LicenseID, x.OccCodeType, x.OccCode });
        });

        modelBuilder.Entity<IngestLog>(e =>
        {
            e.ToTable("ingestlog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.ToTable("apikeys");
            e.HasKey(x => x.KeyId);
            e.Property(x => x.KeyId).HasColumnName("keyid");
            e.Property(x => x.Username).HasColumnName("username");
            e.Property(x => x.KeyHash).HasColumnName("keyhash");
            e.Property(x => x.KeyPrefix).HasColumnName("keyprefix");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("createdat");
            e.Property(x => x.ExpiresAt).HasColumnName("expiresat");
            e.Property(x => x.LastUsedAt).HasColumnName("lastusedat");
            e.Property(x => x.RevokedAt).HasColumnName("revokedat");
            e.Property(x => x.RateProfile).HasColumnName("rateprofile");
        });

        modelBuilder.Entity<Cpi>(e =>
        {
            e.ToTable("cpi");
            e.HasKey(x => new { x.SeriesId, x.Year, x.Period });
        });

        modelBuilder.Entity<CpiSeries>(e =>
        {
            e.ToTable("cpiseries");
            e.HasKey(x => x.SeriesId);
            e.Property(x => x.BeginYear).HasColumnName("beginyear");
            e.Property(x => x.EndYear).HasColumnName("endyear");
            e.Property(x => x.FootnoteCodesStr).HasColumnName("footnotecodesstr");
            e.Property(x => x.SeriesName).HasColumnName("seriesname");
        });

        modelBuilder.Entity<CpiItem>(e =>
        {
            e.ToTable("cpitems");
            e.HasKey(x => x.ItemCode);
        });

        modelBuilder.Entity<CpiArea>(e =>
        {
            e.ToTable("cpiareas");
            e.HasKey(x => x.AreaCode);
        });

        modelBuilder.Entity<CpiPeriodicity>(e =>
        {
            e.ToTable("cpiperiodicities");
            e.HasKey(x => x.PeriodicityCode);
        });

        modelBuilder.Entity<CpiSeasonalAdjustment>(e =>
        {
            e.ToTable("cpiseasonaladjustments");
            e.HasKey(x => x.SeasonalCode);
        });

        // PostgreSQL convention: lowercase all column names so quoted identifiers
        // match the migration scripts without needing quoting in raw SQL.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToLowerInvariant());
            }
        }
    }
}

