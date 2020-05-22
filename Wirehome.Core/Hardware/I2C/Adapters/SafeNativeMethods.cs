using System;
using System.Runtime.InteropServices;

namespace Wirehome.Core.Hardware.I2C.Adapters
{
    internal static class SafeNativeMethods
    {
        [DllImport("libc.so.6", EntryPoint = "syscall", SetLastError = true)]
        public static extern IntPtr Syscall(int id);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("libc.so.6", EntryPoint = "open", SetLastError = true, CharSet = CharSet.Ansi)]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        public static extern int Open(string fileName, int mode);

        [DllImport("libc.so.6", EntryPoint = "ioctl", SetLastError = true)]
        public static extern int Ioctl(int fd, int request, int data);

        [DllImport("libc.so.6", EntryPoint = "read", SetLastError = true)]
        public static extern int Read(int handle, byte[] data, int length, int offset);

        [DllImport("libc.so.6", EntryPoint = "write", SetLastError = true)]
        public static extern int Write(int handle, byte[] data, int length, int offset);
    }
}
