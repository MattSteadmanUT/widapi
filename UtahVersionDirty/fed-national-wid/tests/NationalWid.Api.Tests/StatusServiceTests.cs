using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public sealed class StatusServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_BlanksLastChangedAt_WhenNoRowsActuallyChanged()
    {
        var options = new DbContextOptionsBuilder<WIDDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new WIDDbContext(options);
        db.IngestLogs.Add(new IngestLog
        {
            DataSet = "License",
            RunAt = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
            LastCheckedAt = new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc),
            LastChangedAt = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
            RecordsChanged = 0,
            RecordsAdded = 0,
            TotalRecords = 100,
            Status = "Succeeded"
        });
        await db.SaveChangesAsync();

        var service = new StatusService(db, NullLogger<StatusService>.Instance);

        var history = await service.GetHistoryAsync("License", page: 1, pageSize: 50);

        var item = Assert.Single(history.Items);
        Assert.Equal("License", item.DataSet);
        Assert.Null(item.LastChangedAt);
        Assert.Equal(new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), item.RunAt);
        Assert.Equal(new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc), item.LastCheckedAt);
    }
}
