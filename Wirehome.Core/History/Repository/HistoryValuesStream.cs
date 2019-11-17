using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.History.Repository
{
    public sealed class HistoryValuesStream : IDisposable
    {
        byte[] BeginTokenPrefixBuffer = Encoding.UTF8.GetBytes("b:");
        byte[] EndTokenPrefixBuffer = Encoding.UTF8.GetBytes("e:");
        byte[] ValueTokenPrefixBuffer = Encoding.UTF8.GetBytes("v:");
        
        readonly Stream _source;

        HistoryValueStreamSerializer _serializer = new HistoryValueStreamSerializer();

        public HistoryValuesStream(Stream source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Token CurrentToken
        {
            get; private set;
        }

        public bool EndOfStream => _source.Position == _source.Length;

        public bool BeginningOfStream => _source.Position == 0;

        public void SeekBegin()
        {
            _source.Seek(0, SeekOrigin.Begin);
        }

        public void SeekEnd()
        {
            _source.Seek(0, SeekOrigin.End);
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (EndOfStream)
            {
                CurrentToken = null;
                return false;
            }

            var readBuffer = new byte[1];
            using (var buffer = new MemoryStream(24))
            {
                while (!EndOfStream)
                {
                    await _source.ReadAsync(readBuffer, 0, 1, cancellationToken).ConfigureAwait(false);

                    if (_serializer.IsSeparator(readBuffer[0]))
                    {
                        CurrentToken = ParseToken(new ArraySegment<byte>(buffer.GetBuffer(), 0, (int)buffer.Length));
                        return true;
                    }

                    buffer.Write(readBuffer, 0, 1);
                }
            }

            return false;
        }

        public async Task<bool> MovePreviousAsync(CancellationToken cancellationToken = default)
        {
            if (BeginningOfStream)
            {
                CurrentToken = null;
                return false;
            }

            var separatorsCount = 0;
            var readBuffer = new byte[1];

            while (!BeginningOfStream)
            {
                _source.Seek(-1, SeekOrigin.Current);
                await _source.ReadAsync(readBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
                _source.Seek(-1, SeekOrigin.Current);

                if (_serializer.IsSeparator(readBuffer[0]))
                {
                    separatorsCount++;

                    if (separatorsCount == 2)
                    {
                        _source.Seek(1, SeekOrigin.Current);
                        break;
                    }
                }
            }

            var position = _source.Position;
            var result = await MoveNextAsync().ConfigureAwait(false);
            _source.Position = position;
                       
            return result;
        }
               
        public Task WriteTokenAsync(Token token, CancellationToken cancellationToken = default)
        {
            // TODO: Move to serializer and keep await calls low (add buffer).

            if (token is BeginToken beginToken)
            {
                return WriteAsync(
                    cancellationToken, 
                    BeginTokenPrefixBuffer, 
                    _serializer.SerializeTimeSpan(beginToken.Value), 
                    _serializer.SerializeSeparator());
            }

            if (token is ValueToken valueToken)
            {
                return WriteAsync(
                    cancellationToken,
                    ValueTokenPrefixBuffer,
                    _serializer.SerializeValue(valueToken.Value),
                    _serializer.SerializeSeparator());
            }

            if (token is EndToken endToken)
            {
                return WriteAsync(
                    cancellationToken,
                    EndTokenPrefixBuffer,
                    _serializer.SerializeTimeSpan(endToken.Value),
                    _serializer.SerializeSeparator());
            }

            throw new NotSupportedException("Token is not supported.");
        }

        public async Task WriteElementAsync(TimeSpan begin, string value, TimeSpan end)
        {
            await WriteTokenAsync(new BeginToken(begin)).ConfigureAwait(false);
            await WriteTokenAsync(new ValueToken(value)).ConfigureAwait(false);
            await WriteTokenAsync(new EndToken(end)).ConfigureAwait(false);
        }

        async Task WriteAsync(CancellationToken cancellationToken, params byte[][] buffers)
        {
            foreach(var buffer in buffers)
            {
                await _source.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }   
        }

        Token ParseToken(ArraySegment<byte> source)
        {
            var tokenKey = new ArraySegment<byte>(source.Array, 0, 2);
            var tokenValue = new ArraySegment<byte>(source.Array, 2, source.Count - 2);

            return _serializer.ParseToken(tokenKey, tokenValue);
        }

        public void Dispose()
        {
            _source.Dispose();
        }
    }

    //public class HistoryRepository
    //{
    //    private DbContextOptions _dbContextOptions;

    //    public TimeSpan ComponentStatusOutdatedTimeout { get; set; } = TimeSpan.FromMinutes(6);

    //    public void Initialize()
    //    {
    //        var dbContextOptionsBuilder = new DbContextOptionsBuilder<HistoryDatabaseContext>();
    //        dbContextOptionsBuilder.UseMySql("Server=localhost;Uid=wirehome;Pwd=w1r3h0m3;SslMode=None;Database=WirehomeHistory");

    //        Initialize(dbContextOptionsBuilder.Options);
    //    }

    //    public void Initialize(DbContextOptions options)
    //    {
    //        _dbContextOptions = options ?? throw new ArgumentNullException(nameof(options));

    //        using (var databaseContext = new HistoryDatabaseContext(_dbContextOptions))
    //        {
    //            databaseContext.Database.EnsureCreated();
    //        }
    //    }

    //    public void Delete()
    //    {
    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            databaseContext.Database.EnsureDeleted();
    //        }
    //    }

    //    public async Task UpdateComponentStatusValueAsync(ComponentStatusValue componentStatusValue, CancellationToken cancellationToken)
    //    {
    //        if (componentStatusValue == null) throw new ArgumentNullException(nameof(componentStatusValue));

    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            var latestEntities = await databaseContext.ComponentStatus
    //                .Where(s =>
    //                    s.ComponentUid == componentStatusValue.ComponentUid &&
    //                    s.StatusUid == componentStatusValue.StatusUid &&
    //                    s.NextEntityID == null)
    //                .OrderByDescending(s => s.RangeEnd)
    //                .ThenByDescending(s => s.RangeStart)
    //                .ToListAsync(cancellationToken);

    //            var latestEntity = latestEntities.FirstOrDefault();

    //            if (latestEntities.Count > 1)
    //            {
    //                // TODO: Log broken data.
    //            }

    //            if (latestEntity == null)
    //            {
    //                var newEntry = CreateComponentStatusEntity(componentStatusValue, null);
    //                databaseContext.ComponentStatus.Add(newEntry);
    //            }
    //            else
    //            {
    //                var newestIsObsolete = latestEntity.RangeEnd > componentStatusValue.Timestamp;
    //                if (newestIsObsolete)
    //                {
    //                    return;
    //                }

    //                var latestIsOutdated = componentStatusValue.Timestamp - latestEntity.RangeEnd > ComponentStatusOutdatedTimeout;
    //                var valueHasChanged = !string.Equals(latestEntity.Value, componentStatusValue.Value, StringComparison.Ordinal);

    //                if (valueHasChanged)
    //                {
    //                    var newEntity = CreateComponentStatusEntity(componentStatusValue, latestEntity);
    //                    databaseContext.ComponentStatus.Add(newEntity);

    //                    if (!latestIsOutdated)
    //                    {
    //                        latestEntity.RangeEnd = componentStatusValue.Timestamp;
    //                    }
    //                }
    //                else
    //                {
    //                    if (!latestIsOutdated)
    //                    {
    //                        latestEntity.RangeEnd = componentStatusValue.Timestamp;
    //                    }
    //                    else
    //                    {
    //                        var newEntity = CreateComponentStatusEntity(componentStatusValue, latestEntity);
    //                        databaseContext.ComponentStatus.Add(newEntity);
    //                    }
    //                }
    //            }

    //            await databaseContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    //        }
    //    }

    //    public async Task DeleteComponentStatusHistoryAsync(string componentUid, string statusUid, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken)
    //    {
    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            var query = BuildQuery(databaseContext, componentUid, statusUid, rangeStart, rangeEnd);
    //            databaseContext.ComponentStatus.RemoveRange(query);
    //            await databaseContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    //        }
    //    }

    //    public async Task<List<ComponentStatusEntity>> GetComponentStatusValuesAsync(string componentUid, string statusUid, int maxRowsCount, CancellationToken cancellationToken)
    //    {
    //        if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
    //        if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));

    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            return await databaseContext.ComponentStatus
    //                .AsNoTracking()
    //                .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid)
    //                .OrderBy(s => s.RangeStart)
    //                .ThenBy(s => s.RangeEnd)
    //                .Take(maxRowsCount)
    //                .ToListAsync(cancellationToken).ConfigureAwait(false);
    //        }
    //    }

    //    public async Task<List<ComponentStatusEntity>> GetComponentStatusValuesAsync(
    //        string componentUid, 
    //        string statusUid, 
    //        DateTime rangeStart, 
    //        DateTime rangeEnd, 
    //        int maxRowsCount, 
    //        CancellationToken cancellationToken)
    //    {
    //        if (componentUid == null) throw new ArgumentNullException(nameof(componentUid));
    //        if (statusUid == null) throw new ArgumentNullException(nameof(statusUid));
    //        if (rangeStart > rangeEnd) throw new ArgumentException($"{nameof(rangeStart)} is greater than {nameof(rangeEnd)}");

    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            return await databaseContext.ComponentStatus
    //                .AsNoTracking()
    //                .Where(s => s.ComponentUid == componentUid && s.StatusUid == statusUid)
    //                .Where(s => (s.RangeStart <= rangeEnd && s.RangeEnd >= rangeStart))
    //                .OrderBy(s => s.RangeStart)
    //                .ThenBy(s => s.RangeEnd)
    //                .Take(maxRowsCount)
    //                .ToListAsync(cancellationToken).ConfigureAwait(false);
    //        }
    //    }

    //    public async Task<int> GetRowCountForComponentStatusHistoryAsync(
    //        string componentUid, 
    //        string statusUid, 
    //        DateTime? rangeStart, 
    //        DateTime? rangeEnd, 
    //        CancellationToken cancellationToken)
    //    {
    //        using (var databaseContext = CreateDatabaseContext())
    //        {
    //            var query = BuildQuery(databaseContext, componentUid, statusUid, rangeStart, rangeEnd);
    //            return await query.CountAsync(cancellationToken);
    //        }
    //    }

    //    private static IQueryable<ComponentStatusEntity> BuildQuery(
    //        HistoryDatabaseContext databaseContext,
    //        string componentUid, 
    //        string statusUid,
    //        DateTime? rangeStart,
    //        DateTime? rangeEnd)
    //    {
    //        var query = databaseContext.ComponentStatus.AsQueryable();

    //        if (!string.IsNullOrEmpty(componentUid))
    //        {
    //            query = query.Where(c => c.ComponentUid == componentUid);
    //        }

    //        if (!string.IsNullOrEmpty(statusUid))
    //        {
    //            query = query.Where(c => c.StatusUid == statusUid);
    //        }

    //        if (rangeStart.HasValue)
    //        {
    //            query = query.Where(c => c.RangeEnd >= rangeStart);
    //        }

    //        if (rangeEnd.HasValue)
    //        {
    //            query = query.Where(c => c.RangeStart <= rangeEnd);
    //        }

    //        return query;
    //    }

    //    private HistoryDatabaseContext CreateDatabaseContext()
    //    {
    //        var databaseContext = new HistoryDatabaseContext(_dbContextOptions);

    //        try
    //        {
    //            databaseContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(120));
    //        }
    //        catch (InvalidOperationException)
    //        {
    //            // This exception is thrown in UnitTests.
    //        }

    //        return databaseContext;
    //    }

    //    private static ComponentStatusEntity CreateComponentStatusEntity(
    //        ComponentStatusValue componentStatusValue,
    //        ComponentStatusEntity latestEntity)
    //    {
    //        var newEntity = new ComponentStatusEntity
    //        {
    //            ComponentUid = componentStatusValue.ComponentUid,
    //            StatusUid = componentStatusValue.StatusUid,
    //            Value = componentStatusValue.Value,
    //            RangeStart = componentStatusValue.Timestamp,
    //            RangeEnd = componentStatusValue.Timestamp,
    //            PreviousEntityID = latestEntity?.ID
    //        };

    //        if (latestEntity != null)
    //        {
    //            latestEntity.NextEntity = newEntity;
    //        }

    //        return newEntity;
    //    }
    //}
}
