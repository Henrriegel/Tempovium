using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tempovium.Infrastructure.Persistence;

public class TempoviumDbContextFactory : IDesignTimeDbContextFactory<TempoviumDbContext>
{
    public TempoviumDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TempoviumDbContext>();

        optionsBuilder.UseSqlite("Data Source=tempovium.db");

        return new TempoviumDbContext(optionsBuilder.Options);
    }
}