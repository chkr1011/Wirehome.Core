using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Python;

public sealed class PythonIOToLogStream : Stream
{
    readonly ILogger _logger;

    public PythonIOToLogStream(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool CanRead { get; } = false;

    public override bool CanSeek { get; } = false;

    public override bool CanWrite { get; } = true;

    public override long Length { get; } = 0;

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
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

        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        var text = Encoding.UTF8.GetString(buffer, offset, count);
        if (text.Equals(Environment.NewLine, StringComparison.Ordinal))
        {
            return;
        }

        _logger.LogDebug(text);
    }
}