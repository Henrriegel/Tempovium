using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tempovium.Infrastructure.Persistence;

public class TempoviumDbContextFactory : IDesignTimeDbContextFactory<TempoviumDbContext>
{
    public TempoviumDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TempoviumDbContext>();

        optionsBuilder.UseSqlite(TempoviumDataPaths.GetSqliteConnectionString());

        return new TempoviumDbContext(optionsBuilder.Options);
    }
}
