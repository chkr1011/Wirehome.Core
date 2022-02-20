using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace Wirehome.Core.Diagnostics.Log;

public sealed class LogReceiver
{
    // UDP can only handle ~64 KB of data!
    readonly ArraySegment<byte> _receiveBuffer = new(new byte[66000]);
    readonly Socket _socket = new(SocketType.Dgram, ProtocolType.Udp);

    public LogReceiver(int port)
    {
        _socket.Bind(new IPEndPoint(IPAddress.Any, port));
    }

    public async Task<LogEntry> ReceiveNextLogEntryAsync(CancellationToken cancellationToken)
    {
        EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
        var receiveResult = await _socket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, endpoint, cancellationToken);

        return MessagePackSerializer.Deserialize<LogEntry>(_receiveBuffer.AsMemory(0, receiveResult.ReceivedBytes));
    }
}