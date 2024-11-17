// ReSharper disable UseObjectOrCollectionInitializer

using System.Buffers.Binary;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using MeshTool;

var rootCommand = new RootCommand("MESH tool");
rootCommand.AddGlobalOption(new Option<string>(["--address", "-a"], "Address"));

// LED
var leCommand = new Command("le", "LED");
leCommand.AddOption(new Option<string>(["--color", "-c"], () => "FFFFFF"));
leCommand.AddOption(new Option<int>(["--time", "-t"], () => -1));
leCommand.AddOption(new Option<int>(["--on", "-n"], () => -1));
leCommand.AddOption(new Option<int>(["--off", "-f"], () => 0));
leCommand.AddOption(new Option<byte>(["--pattern", "-p"], () => 1));
leCommand.Handler = CommandHandler.Create(async (string address, string color, int time, int on, int off, byte pattern) =>
{
    var rgb = Convert.ToInt32(color, 16);

    using var device = await MeshHelper.DiscoverDeviceAsync("MESH-100LE", address).ConfigureAwait(false);
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
        pattern,
        0x00
    };
    WriteTime(command.AsSpan(7), time);
    WriteTime(command.AsSpan(9), on);
    WriteTime(command.AsSpan(11), off);
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
});
rootCommand.Add(leCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
