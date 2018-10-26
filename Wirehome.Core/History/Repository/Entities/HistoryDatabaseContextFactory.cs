using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wirehome.Core.History.Repository.Entities
{
    // ReSharper disable once UnusedMember.Global
    public class HistoryDatabaseContextFactory : IDesignTimeDbContextFactory<HistoryDatabaseContext>
    {
        public HistoryDatabaseContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            optionsBuilder.UseMySql("Data Source=dummy");

            return new HistoryDatabaseContext(optionsBuilder.Options);
        }
    }
}