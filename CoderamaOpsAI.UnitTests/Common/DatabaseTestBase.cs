using CoderamaOpsAI.Dal;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.UnitTests.Common;

public abstract class DatabaseTestBase : IDisposable
{
    protected readonly AppDbContext DbContext;

    protected DatabaseTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options);
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
