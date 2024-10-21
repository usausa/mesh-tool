// ReSharper disable UseObjectOrCollectionInitializer
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using MeshTool;

var rootCommand = new RootCommand("MESH tool");
rootCommand.AddGlobalOption(new Option<string>(["--address", "-a"], "Address"));

// LED
var leCommand = new Command("le", "LED");
// TODO color, time, on, off, pattern
leCommand.Handler = CommandHandler.Create(async (string address) =>
{
    using var device = await MeshHelper.DiscoverDeviceAsync("MESH-100LE", address).ConfigureAwait(false);
    if (device is null)
    {
        Console.WriteLine("Device not found.");
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
        0x00, // Message Type
        0x02, // Event Type LED
        0x00, // R
        0x00,
        0xFF, // G
        0x00,
        0x00, // B
        0x88, 0x13, // 点灯時間
        0x64, 0x00, // 点灯サイクル(100ms)
        0x64, 0x00, // 消灯サイクル(100ms)
        0x01, // 点灯パターン
        0x00
    };
    command[^1] = MeshHelper.CalcCrc(command.AsSpan(0, command.Length - 1));
    if (!await MeshHelper.WriteCommandAsync(characteristic, command).ConfigureAwait(false))
    {
        Console.WriteLine("Write command failed.");
    }
});
rootCommand.Add(leCommand);

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
