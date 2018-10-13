using Microsoft.EntityFrameworkCore;

namespace Wirehome.Core.History.Repository.Entities
{
    public class HistoryDatabaseContext : DbContext
    {
        public HistoryDatabaseContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ComponentStatusEntity> ComponentStatus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ComponentStatusEntity>()
                .HasIndex(b => new { b.RangeStart, b.RangeEnd, b.ComponentUid, b.StatusUid });
        }
    }
}
