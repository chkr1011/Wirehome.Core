using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Wirehome.Core.Python
{
    public class PythonIOToLogStream : Stream
    {
        private readonly ILogger _logger;

        public PythonIOToLogStream(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var text = Encoding.UTF8.GetString(buffer, 0, count);
            if (text.Equals(Environment.NewLine))
            {
                return;
            }

            _logger.LogDebug(text);
        }

        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get; }

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
