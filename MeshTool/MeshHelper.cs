namespace MeshTool;

using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

public static class MeshHelper
{
    private static readonly Guid MeshService = new("72c90001-57a9-4d40-b746-534e22ec9f9e");
    private static readonly Guid WriteCharacteristic = new("72c90002-57a9-4d40-b746-534e22ec9f9e");

    public static async ValueTask<BluetoothLEDevice?> DiscoverDeviceAsync(string name, string address)
    {
        var tcs = new TaskCompletionSource<BluetoothLEDevice?>();
        var bluetoothAddress = String.IsNullOrEmpty(address) ? 0 : Convert.ToUInt64(address, 16);

        // Watcher
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        // Start
        watcher.Received += ReceivedHandler;
        watcher.Start();

        // Discover
        var target = await tcs.Task.ConfigureAwait(false);

        // Stop
        watcher.Stop();
        watcher.Received -= ReceivedHandler;

        return target;

        async void ReceivedHandler(BluetoothLEAdvertisementWatcher source, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            if ((bluetoothAddress != 0) && (eventArgs.BluetoothAddress != bluetoothAddress))
            {
                return;
            }

            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
            if ((device is not null) && device.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                tcs.TrySetResult(device);
            }
        }
    }

    public static async ValueTask<GattCharacteristic?> ResolveWriteCharacteristic(BluetoothLEDevice device)
    {
#pragma warning disable CA1031
        try
        {
            var getServiceResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (getServiceResult.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            var service = getServiceResult.Services.FirstOrDefault(static x => x.Uuid == MeshService);
            if (service is null)
            {
                return null;
            }

            var requestAccessResult = await service.RequestAccessAsync();
            if (requestAccessResult != DeviceAccessStatus.Allowed)
            {
                return null;
            }

            var getCharacteristicResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (getCharacteristicResult.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            return getCharacteristicResult.Characteristics.FirstOrDefault(static x => x.Uuid == WriteCharacteristic);
        }
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031
    }

    public static ValueTask<bool> InitializeMeshAsync(GattCharacteristic characteristic)
    {
        var command = new byte[]
        {
            0x00, // Message Type
            0x02, // Event Type
            0x01, // Enable
            0x03  // Checksum
        };
        return WriteCommandAsync(characteristic, command);
    }

    public static async ValueTask<bool> WriteCommandAsync(GattCharacteristic characteristic, byte[] command)
    {
        using var writer = new DataWriter();
        writer.WriteBytes(command);

        var result = await characteristic.WriteValueWithResultAsync(writer.DetachBuffer());
        return result.Status == GattCommunicationStatus.Success;
    }

    public static byte CalcCrc(ReadOnlySpan<byte> span)
    {
        var sum = 0;
        foreach (var b in span)
        {
            sum += b;
        }
        return (byte)(sum & 0xFF);
    }
}
