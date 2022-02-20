using System.IO;
using System.Text;

namespace Wirehome.Core.Hardware.GPIO.Adapters;

public sealed class Gpio
{
    readonly string _directionPath;
    readonly byte[] _readBuffer = new byte[1];
    readonly string _valuePath;

    public Gpio(int id)
    {
        Id = id;

        _valuePath = $"/sys/class/gpio/gpio{id}/value";
        _directionPath = $"/sys/class/gpio/gpio{id}/direction";
    }

    public int Id { get; }

    public GpioState Read()
    {
        using (var fileStream = File.OpenRead(_valuePath))
        {
            fileStream.Read(_readBuffer, 0, 1);

            // ASCII '1' is DEC 49.
            if (_readBuffer[0] == 49)
            {
                return GpioState.High;
            }

            return GpioState.Low;
        }
    }

    public void SetDirection(GpioDirection direction)
    {
        // Edge is only required if the state is read via blocked thread.
        //File.WriteAllText("/sys/class/gpio/gpio" + gpioId + "/edge", edge.ToString().ToLowerInvariant());

        var fileContent = direction == GpioDirection.Output ? "out" : "in";
        File.WriteAllText(_directionPath, fileContent, Encoding.ASCII);
    }

    public void Write(GpioState state)
    {
        using (var fileStream = File.OpenWrite(_valuePath))
        {
            if (state == GpioState.Low)
            {
                // ASCII '0' is DEC 48.
                fileStream.WriteByte(48);
            }
            else
            {
                // ASCII '1' is DEC 49.
                fileStream.WriteByte(49);
            }
        }
    }
}