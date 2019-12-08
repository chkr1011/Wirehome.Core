using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.History.Repository
{
    public sealed class HistoryValuesStream : IDisposable
    {
        private const int TokenBufferSize = 24;

        private readonly Stream _stream;

        private HistoryValueStreamSerializer _serializer = new HistoryValueStreamSerializer();

        public HistoryValuesStream(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public Token CurrentToken
        {
            get; private set;
        }

        public bool EndOfStream => _stream.Position == _stream.Length;

        public bool BeginningOfStream => _stream.Position == 0;

        public void SeekBegin()
        {
            _stream.Seek(0, SeekOrigin.Begin);
        }

        public void SeekEnd()
        {
            _stream.Seek(0, SeekOrigin.End);
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
        {
            if (EndOfStream)
            {
                CurrentToken = null;
                return false;
            }

            var readBufferMemory = ArrayPool<byte>.Shared.Rent(1);
            try
            {
                using (var buffer = new MemoryStream(TokenBufferSize))
                {
                    while (!EndOfStream)
                    {
                        await _stream.ReadAsync(readBufferMemory, 0, 1, cancellationToken).ConfigureAwait(false);

                        if (_serializer.IsSeparator(readBufferMemory[0]))
                        {
                            CurrentToken = ParseToken(buffer.GetBuffer().AsSpan().Slice(0, (int)buffer.Length));
                            return true;
                        }

                        buffer.Write(readBufferMemory, 0, 1);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBufferMemory);
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
            var readBufferMemory = ArrayPool<byte>.Shared.Rent(1);
            try
            {
                while (!BeginningOfStream)
                {
                    _stream.Seek(-1, SeekOrigin.Current);
                    await _stream.ReadAsync(readBufferMemory, 0, 1, cancellationToken).ConfigureAwait(false);
                    _stream.Seek(-1, SeekOrigin.Current);

                    if (_serializer.IsSeparator(readBufferMemory[0]))
                    {
                        separatorsCount++;

                        if (separatorsCount == 2)
                        {
                            _stream.Seek(1, SeekOrigin.Current);
                            break;
                        }
                    }
                }

                var position = _stream.Position;
                var result = await MoveNextAsync().ConfigureAwait(false);
                _stream.Position = position;

                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBufferMemory);
            }
        }

        public async Task<bool> Align(CancellationToken cancellationToken = default)
        {
            while (!EndOfStream)
            {
                var readBufferMemory = ArrayPool<byte>.Shared.Rent(1);
                try
                {
                    await _stream.ReadAsync(readBufferMemory, 0, 1, cancellationToken).ConfigureAwait(false);
                    if (_serializer.IsSeparator(readBufferMemory[0]))
                    {
                        return true;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(readBufferMemory);
                }
            }

            return false;
        }

        public async Task WriteTokenAsync(Token token, CancellationToken cancellationToken = default)
        {
            // [    Token    ]
            // [i][content][s]
            //
            // i = ID (1 byte)
            // content = (n bytes)
            // s = Separator (1 byte)

            using (var buffer = new MemoryStream(TokenBufferSize))
            {
                if (token is BeginToken beginToken)
                {
                    buffer.Write(_serializer.SerializeBeginTokenId());
                    buffer.Write(_serializer.SerializeTimeSpan(beginToken.Value));
                }
                else if (token is ValueToken valueToken)
                {
                    buffer.Write(_serializer.SerializeValueTokenId());
                    buffer.Write(_serializer.SerializeValue(valueToken.Value));
                }
                else if (token is EndToken endToken)
                {
                    buffer.Write(_serializer.SerializeEndTokenId());
                    buffer.Write(_serializer.SerializeTimeSpan(endToken.Value));
                }
                else
                {
                    throw new NotSupportedException("Token is not supported.");
                }

                buffer.Write(_serializer.SerializeSeparator());

                await _stream.WriteAsync(buffer.GetBuffer().AsMemory().Slice(0, (int)buffer.Length), cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        private Token ParseToken(ReadOnlySpan<byte> source)
        {
            var tokenKey = source.Slice(0, 1);
            var tokenValue = source.Slice(1, source.Length - 1);

            return _serializer.ParseToken(tokenKey, tokenValue);
        }
    }
}