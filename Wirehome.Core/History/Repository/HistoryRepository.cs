using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Wirehome.Core.History.Repository.Entities;

namespace Wirehome.Core.History.Repository
{
    public class HistoryRepository
    {
        private DbContextOptions _dbContextOptions;

        // TODO: Consider caching the latest entries for fast comparison.
        //private readonly Dictionary<string, ComponentStatusValue> _latestComponentStatusValues = new Dictionary<string, ComponentStatusValue>();

        public TimeSpan ComponentStatusOutdatedTimeout { get; set; } = TimeSpan.FromMinutes(6);

        public void Initialize()
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            dbContextOptionsBuilder.UseMySql("Server=localhost;Uid=wirehome;Pwd=w1r3h0m3;SslMode=None;Database=WirehomeHistory");

            _dbContextOptions = dbContextOptionsBuilder.Options;

            Initialize(_dbContextOptions);
        }

        public void Delete()
        {
            using (var databaseContext = CreateDatabaseContext())
            {
                databaseContext.Database.EnsureDeleted();
            }
        }

        public void Initialize(DbContextOptions options)
        {
            using (var databaseContext = new HistoryDatabaseContext(options))
            {
                databaseContext.Database.EnsureCreated();
            }
        }

        public void UpdateComponentStatusValue(ComponentStatusValue componentStatusValue)
        {
            if (componentStatusValue == null) throw new ArgumentNullException(nameof(componentStatusValue));

            using (var databaseContext = CreateDatabaseContext())
            {
                // TODO: Check next entity instead of ordering.

                var latestEntity = databaseContext.ComponentStatus
                    .OrderByDescending(s => s.RangeEnd)
                    .ThenByDescending(s => s.RangeStart)
                    .FirstOrDefault(s =>
                        s.ComponentUid == componentStatusValue.ComponentUid &&
                        s.StatusUid == componentStatusValue.StatusUid);

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
                    var valueHasChanged = string.CompareOrdinal(latestEntity.Value, componentStatusValue.Value) != 0;

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

                databaseContext.SaveChanges();
            }
        }

        public List<ComponentStatusEntity> GetComponentStatusValues(string componentUid, string statusUid)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

            using (var databaseContext = CreateDatabaseContext())
            {
                return databaseContext.ComponentStatus
                    .AsNoTracking()
                    .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid).ToList();
            }
        }

        public List<ComponentStatusEntity> GetComponentStatusValues(string componentUid, string statusUid, DateTimeOffset rangeStart, DateTimeOffset rangeEnd)
        {
            if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
            if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));
            if (rangeStart > rangeEnd) throw new ArgumentException($"{nameof(rangeStart)} is greater than {nameof(rangeEnd)}");

            using (var databaseContext = CreateDatabaseContext())
            {
                return databaseContext.ComponentStatus
                    .AsNoTracking()
                    .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid)
                    .Where(s => (s.RangeStart <= rangeEnd && s.RangeEnd >= rangeStart))
                    .OrderBy(s => s.RangeStart)
                    .ToList();
            }
        }

        private HistoryDatabaseContext CreateDatabaseContext()
        {
            return new HistoryDatabaseContext(_dbContextOptions);
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
