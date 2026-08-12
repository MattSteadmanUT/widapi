using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Tests;

public class UnitTest1
{
    [Fact]
    public void GetOrderedMigrationFiles_ReturnsOrdinalFileOrder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wid-migrations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "003_seed_lookups.sql"), "--seed");
            File.WriteAllText(Path.Combine(tempDir, "001_create_schema.sql"), "--schema");
            File.WriteAllText(Path.Combine(tempDir, "002_create_indexes.sql"), "--indexes");

            var files = MigrationService.GetOrderedMigrationFiles(tempDir)
                .Select(path => Path.GetFileName(path)!)
                .ToArray();

            Assert.Equal(
            [
                "001_create_schema.sql",
                "002_create_indexes.sql",
                "003_seed_lookups.sql",
            ],
            files);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetRequiredTables_ContainsCoreTables()
    {
        var requiredTables = MigrationService.GetRequiredTables();

        Assert.Contains("laborforce", requiredTables);
        Assert.Contains("ces", requiredTables);
        Assert.Contains("industry", requiredTables);
        Assert.Contains("iowage", requiredTables);
        Assert.Contains("projectionsmatrix", requiredTables);
        Assert.Contains("geographies", requiredTables);
        Assert.Contains("periodyears", requiredTables);
        Assert.Contains("ingestlog", requiredTables);
    }
}
