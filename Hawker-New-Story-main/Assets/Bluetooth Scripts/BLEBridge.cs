using System;
using UnityEngine;

public static class BLEBridge
{
    public static Action<string> UILog = s => Debug.Log(s);

    // -------- Init/Quit --------
    public static void Initialize(Action ok, Action<string> err)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UILog("[Android] BLE init ...");
        try
        {
            BluetoothLEHardwareInterface.Initialize(
                asCentral: true,
                asPeripheral: false,
                action: () => { UILog("[Android] Init OK"); ok?.Invoke(); },
                errorAction: (e) => { UILog("[Android] Init FAIL: " + e); err?.Invoke(e); }
            );
        }
        catch (Exception ex)
        {
            UILog("[Android] Init EXCEPTION: " + ex.Message);
            err?.Invoke(ex.Message);
        }
#else
        UILog("[Editor] Mock init");
        ok?.Invoke();
#endif
    }

    public static void Quit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            BluetoothLEHardwareInterface.DeInitialize(() =>
            {
                UILog("[Android] BLE deinitialized");
            });
        }
        catch { }
#endif
    }

    // -------- Scan --------
    public static void ScanForPeripheralsWithServices(
        string[] serviceUUIDs,
        Action<string, string> onFoundSimple,
        Action<string, string, int, byte[]> onFoundAdv,
        bool rssiOnly)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UILog("[Android] Start Scan ...");
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            serviceUUIDs,
            (addr, name) => { onFoundSimple?.Invoke(addr, name); },
            (addr, name, rssi, data) => { onFoundAdv?.Invoke(addr, name, rssi, data); },
            rssiOnly
        );
#else
        UILog("[Editor] Skip scan (mock)");
#endif
    }

    public static void StopScan()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        BluetoothLEHardwareInterface.StopScan();
        UILog("[Android] Stop Scan");
#endif
    }

    // -------- Connect --------
    public static void ConnectToPeripheral(
        string address,
        Action<string> onConnected,
        Action<string, string> onService,
        Action<string, string, string> onCharacteristic)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        UILog("[Android] Connecting: " + address);
        BluetoothLEHardwareInterface.ConnectToPeripheral(
            address,
            (a) => { onConnected?.Invoke(a); },
            (a, service) => { onService?.Invoke(a, service); },
            (a, service, characteristic) => { onCharacteristic?.Invoke(a, service, characteristic); }
        );
#else
        UILog("[Editor] Skip connect (mock)");
#endif
    }

    // -------- Write --------
    public static void WriteCharacteristic(
        string address, string serviceUuid, string characteristicUuid,
        byte[] data, int length, bool withResponse, Action<string> onWrite)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        BluetoothLEHardwareInterface.WriteCharacteristic(
            address, serviceUuid, characteristicUuid,
            data, length, /*force*/ true,
            (ch) => { UILog($"[Android] Write OK ch={ch} val=0x{data[0]:X2}"); onWrite?.Invoke(ch); }
        );
#else
        UILog($"[Editor] Mock write val=0x{data[0]:X2}");
        onWrite?.Invoke(characteristicUuid);
#endif
    }
}


