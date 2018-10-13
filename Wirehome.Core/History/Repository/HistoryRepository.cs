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

        private TimeSpan _autoRefreshInterval = TimeSpan.FromMinutes(1);

        public HistoryRepository()
        {
        }

        public void Initialize()
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
            dbContextOptionsBuilder.UseMySql("Server=localhost;Uid=wirehome;Pwd=w1r3h0m3;SslMode=None;Database=WirehomeHistory");

            _dbContextOptions = dbContextOptionsBuilder.Options;

            Initialize(_dbContextOptions);
        }

        private HistoryDatabaseContext CreateDatabaseContext()
        {
            return new HistoryDatabaseContext(_dbContextOptions);
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
                var latestEntity = databaseContext.ComponentStatus
                    .OrderByDescending(s => s.RangeEnd)
                    .FirstOrDefault(s =>
                        s.ComponentUid == componentStatusValue.ComponentUid &&
                        s.StatusUid == componentStatusValue.StatusUid);

                if (latestEntity == null)
                {
                    var newEntry = CreateNewComponentStatusEntity(componentStatusValue);
                    databaseContext.ComponentStatus.Add(newEntry);
                }
                else
                {
                    var isOutdated = componentStatusValue.Timestamp - latestEntity.RangeEnd > _autoRefreshInterval;

                    if (!isOutdated && string.CompareOrdinal(latestEntity.Value, componentStatusValue.Value) == 0)
                    {
                        latestEntity.RangeEnd = componentStatusValue.Timestamp;
                    }
                    else
                    {
                        latestEntity.IsLatest = false;

                        var newEntry = CreateNewComponentStatusEntity(componentStatusValue);
                        newEntry.PredecessorID = latestEntity.ID;

                        databaseContext.ComponentStatus.Add(newEntry);
                    }
                }

                databaseContext.SaveChanges();
            }
        }

        public ComponentStatusEntity GetLatestComponentStatusValue(string componentUid, string statusUid)
        {
            using (var databaseContext = CreateDatabaseContext())
            {
                return databaseContext.ComponentStatus
                    .AsNoTracking()
                    .FirstOrDefault(s => s.ComponentUid == componentUid && s.StatusUid == statusUid && s.IsLatest);
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
                    .Where(s => (s.RangeStart < rangeEnd && s.RangeEnd > rangeStart))
                    .OrderByDescending(s => s.RangeEnd)
                    .ToList();
            }
        }

        private static ComponentStatusEntity CreateNewComponentStatusEntity(ComponentStatusValue message)
        {
            return new ComponentStatusEntity
            {
                ComponentUid = message.ComponentUid,
                StatusUid = message.StatusUid,
                Value = message.Value,
                RangeStart = message.Timestamp,
                RangeEnd = message.Timestamp,
                IsLatest = true
            };
        }
    }
}
