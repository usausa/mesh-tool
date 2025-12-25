// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
namespace MeshTool;

using System.Buffers.Binary;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<LedCommand>();
    }
}

// LED
[Command("led", "LED")]
public sealed class LedCommand : ICommandHandler
{
    [Option<string>("--address", "-a", Description = "Address", Required = true)]
    public string Address { get; set; } = default!;

    [Option<string>("--color", "-c", Description = "Address", DefaultValue = "FFFFFF")]
    public string Color { get; set; } = default!;

    [Option<int>("--time", "-t", Description = "Time", DefaultValue = -1)]
    public int Time { get; set; }

    [Option<int>("--on", "-n", Description = "ON", DefaultValue = -1)]
    public int On { get; set; }

    [Option<int>("--off", "-f", Description = "OFF", DefaultValue = 0)]
    public int Off { get; set; }

    [Option<byte>("--pattern", "-p", Description = "Pattern", DefaultValue = 1)]
    public byte Pattern { get; set; }

    public async ValueTask ExecuteAsync(CommandContext context)
    {
        var rgb = Convert.ToInt32(Color, 16);

        using var device = await MeshHelper.DiscoverDeviceAsync("MESH-100LE", Address).ConfigureAwait(false);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return;
        }

        if (!await MeshHelper.PairAsync(device).ConfigureAwait(false))
        {
            Console.WriteLine("Pairing failed.");
            return;
        }

        var characteristic = await MeshHelper.ResolveWriteCharacteristic(device).ConfigureAwait(false);
        if (characteristic is null)
        {
            Console.WriteLine("Service resolve failed.");
            return;
        }

        if (!await MeshHelper.InitializeMeshAsync(characteristic).ConfigureAwait(false))
        {
            Console.WriteLine("Initialize mesh failed.");
            return;
        }

        var command = new byte[]
        {
            0x01,
            0x00,
            (byte)((rgb >> 16) & 0xff),
            0x00,
            (byte)((rgb >> 8) & 0xff),
            0x00,
            (byte)(rgb & 0xff),
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            Pattern,
            0x00
        };
        WriteTime(command.AsSpan(7), Time);
        WriteTime(command.AsSpan(9), On);
        WriteTime(command.AsSpan(11), Off);
        command[^1] = MeshHelper.CalcCrc(command.AsSpan(0, command.Length - 1));
        if (!await MeshHelper.WriteCommandAsync(characteristic, command).ConfigureAwait(false))
        {
            Console.WriteLine("Write command failed.");
        }

        return;

        static void WriteTime(Span<byte> buffer, int value)
        {
            BinaryPrimitives.TryWriteUInt16LittleEndian(buffer, (ushort)(value is < 0 or > 65535 ? 65535 : value));
        }
    }
}
