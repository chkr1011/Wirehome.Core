using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.History.Repository.Entities;

namespace Wirehome.Core.History.Repository
{
    public class HistoryRepository
    {
        private DbContextOptions _dbContextOptions;

        public TimeSpan ComponentStatusOutdatedTimeout { get; set; } = TimeSpan.FromMinutes(6);

        public void Initialize()
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            dbContextOptionsBuilder.UseMySql("Server=localhost;Uid=wirehome;Pwd=w1r3h0m3;SslMode=None;Database=WirehomeHistory");

            Initialize(dbContextOptionsBuilder.Options);
        }

        public void Initialize(DbContextOptions options)
        {
            _dbContextOptions = options ?? throw new ArgumentNullException(nameof(options));

            using (var databaseContext = new HistoryDatabaseContext(_dbContextOptions))
            {
                databaseContext.Database.EnsureCreated();
            }
        }

        public void Delete()
        {
            using (var databaseContext = CreateDatabaseContext())
            {
                databaseContext.Database.EnsureDeleted();
            }
        }

        public async Task UpdateComponentStatusValueAsync(ComponentStatusValue componentStatusValue, CancellationToken cancellationToken)
        {
            if (componentStatusValue == null) throw new ArgumentNullException(nameof(componentStatusValue));

            using (var databaseContext = CreateDatabaseContext())
            {
                var latestEntities = await databaseContext.ComponentStatus
                    .Where(s =>
                        s.ComponentUid == componentStatusValue.ComponentUid &&
                        s.StatusUid == componentStatusValue.StatusUid &&
                        s.NextEntityID == null)
                    .OrderByDescending(s => s.RangeEnd)
                    .ThenByDescending(s => s.RangeStart)
                    .ToListAsync(cancellationToken);

                var latestEntity = latestEntities.FirstOrDefault();

                if (latestEntities.Count > 1)
                {
                    // TODO: Log broken data.
                }

                if (latestEntity == null)
                {
                    var newEntry = CreateComponentStatusEntity(componentStatusValue, null);
                    databaseContext.ComponentStatus.Add(newEntry);
                }
                else
                {
                    var newestIsObsolete = latestEntity.RangeEnd > componentStatusValue.Timestamp;
                    if (newestIsObsolete)
                    {
                        return;
                    }

                    var latestIsOutdated = componentStatusValue.Timestamp - latestEntity.RangeEnd > ComponentStatusOutdatedTimeout;
                    var valueHasChanged = !string.Equals(latestEntity.Value, componentStatusValue.Value, StringComparison.Ordinal);

                    if (valueHasChanged)
                    {
                        var newEntity = CreateComponentStatusEntity(componentStatusValue, latestEntity);
                        databaseContext.ComponentStatus.Add(newEntity);

                        if (!latestIsOutdated)
                        {
                            latestEntity.RangeEnd = componentStatusValue.Timestamp;
                        }
                    }
                    else
                    {
                        if (!latestIsOutdated)
                        {
                            latestEntity.RangeEnd = componentStatusValue.Timestamp;
                        }
                        else
                        {
                            var newEntity = CreateComponentStatusEntity(componentStatusValue, latestEntity);
                            databaseContext.ComponentStatus.Add(newEntity);
                        }
                    }
                }

                await databaseContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DeleteComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
        {
            using (var databaseContext = CreateDatabaseContext())
            {
                var query = BuildQuery(databaseContext, componentUid, statusUid, rangeStart, rangeEnd);
                databaseContext.ComponentStatus.RemoveRange(query);
                await databaseContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<List<ComponentStatusEntity>> GetComponentStatusValuesAsync(string componentUid, string statusUid, int maxRowsCount, CancellationToken cancellationToken)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            using (var databaseContext = CreateDatabaseContext())
            {
                return await databaseContext.ComponentStatus
                    .AsNoTracking()
                    .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid)
                    .OrderBy(s => s.RangeStart)
                    .ThenBy(s => s.RangeEnd)
                    .Take(maxRowsCount)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<List<ComponentStatusEntity>> GetComponentStatusValuesAsync(
            string componentUid, 
            string statusUid, 
            DateTime rangeStart, 
            DateTime rangeEnd, 
            int maxRowsCount, 
            CancellationToken cancellationToken)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));
            if (rangeStart > rangeEnd) throw new ArgumentException($"{nameof(rangeStart)} is greater than {nameof(rangeEnd)}");

            using (var databaseContext = CreateDatabaseContext())
            {
                return await databaseContext.ComponentStatus
                    .AsNoTracking()
                    .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid)
                    .Where(s => (s.RangeStart <= rangeEnd && s.RangeEnd >= rangeStart))
                    .OrderBy(s => s.RangeStart)
                    .ThenBy(s => s.RangeEnd)
                    .Take(maxRowsCount)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<int> GetRowCountForComponentStatusHistoryAsync(
            string componentUid, 
            string statusUid, 
            DateTime? rangeStart, 
            DateTime? rangeEnd, 
            CancellationToken cancellationToken)
        {
            using (var databaseContext = CreateDatabaseContext())
            {
                var query = BuildQuery(databaseContext, componentUid, statusUid, rangeStart, rangeEnd);
                return await query.CountAsync(cancellationToken);
            }
        }

        private static IQueryable<ComponentStatusEntity> BuildQuery(
            HistoryDatabaseContext databaseContext,
            string componentUid, 
            string statusUid,
            DateTime? rangeStart,
            DateTime? rangeEnd)
        {
            var query = databaseContext.ComponentStatus.AsQueryable();

            if (!string.IsNullOrEmpty(componentUid))
            {
                query = query.Where(c => c.ComponentUid == componentUid);
            }

            if (!string.IsNullOrEmpty(statusUid))
            {
                query = query.Where(c => c.StatusUid == statusUid);
            }

            if (rangeStart.HasValue)
            {
                query = query.Where(c => c.RangeEnd >= rangeStart);
            }

            if (rangeEnd.HasValue)
            {
                query = query.Where(c => c.RangeStart <= rangeEnd);
            }

            return query;
        }
        
        private HistoryDatabaseContext CreateDatabaseContext()
        {
            var databaseContext = new HistoryDatabaseContext(_dbContextOptions);

            try
            {
                databaseContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(120));
            }
            catch (InvalidOperationException)
            {
                // This exception is thrown in UnitTests.
            }
            
            return databaseContext;
        }

        private static ComponentStatusEntity CreateComponentStatusEntity(
            ComponentStatusValue componentStatusValue,
            ComponentStatusEntity latestEntity)
        {
            var newEntity = new ComponentStatusEntity
            {
                ComponentUid = componentStatusValue.ComponentUid,
                StatusUid = componentStatusValue.StatusUid,
                Value = componentStatusValue.Value,
                RangeStart = componentStatusValue.Timestamp,
                RangeEnd = componentStatusValue.Timestamp,
                PreviousEntityID = latestEntity?.ID
            };

            if (latestEntity != null)
            {
                latestEntity.NextEntity = newEntity;
            }

            return newEntity;
        }
    }
}
