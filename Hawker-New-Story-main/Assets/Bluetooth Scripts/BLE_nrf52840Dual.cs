using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BLE_nrf52840Dual : MonoBehaviour
{
    [Header("Board A")]
    public string DeviceNameA = "RightHand_Custom";
    public string ServiceUUIDA = "29C20000-E8F2-537E-4F6C-D104768A2214";
    public string CharacteristicUUIDA = "29C20001-E8F2-537E-4F6C-D104768A2214";

    [Header("Board B")]
    public string DeviceNameB = "LeftHand_Custom";
    public string ServiceUUIDB = "19B10000-E8F2-537E-4F6C-D104768A1214";
    public string CharacteristicUUIDB = "19B10001-E8F2-537E-4F6C-D104768A1214";

    [Header("World-Space Text for BLE logs (only BLE flow)")]
    public Text StatusText;

    // ---------- singleton / guards ----------
    public static BLE_nrf52840Dual Instance;
    private bool _booted = false;          // guard Start()
    private string _idTag = "";            // prefix to identify which instance prints

    public static Action<string> StaticLog = _ => { };

    [Serializable]
    private class Board
    {
        public string Key;                 // "A"/"B"
        public string DeviceName;          // Adv name
        public string ServiceUUID;         // Target service
        public string CharacteristicUUID;  // Target writable characteristic
        public string Address;             // Discovered address
        public bool Found;                 // Discovered by scan
        public bool IsConnected;           // Char confirmed (ready to write)
    }

    private Dictionary<string, Board> boards;

    private enum State { None, Scanning, Ready }
    private State _state = State.None;
    private float _scanTimeout;
    private float _scanTicker;

    private string StatusMessage
    {
        set
        {
            var msg = _idTag + value;
            BLEBridge.UILog?.Invoke(msg);
            if (StatusText) StatusText.text = msg;
            Debug.Log(msg);
        }
    }

    void Awake()
    {
        // ---------- singleton ----------
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[BLE Dual] Duplicate instance on {name}, destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _idTag = $"[Dual#{GetInstanceID()}] ";

        boards = new Dictionary<string, Board>
        {
            ["A"] = new Board { Key="A", DeviceName=DeviceNameA, ServiceUUID=ServiceUUIDA, CharacteristicUUID=CharacteristicUUIDA },
            ["B"] = new Board { Key="B", DeviceName=DeviceNameB, ServiceUUID=ServiceUUIDB, CharacteristicUUID=CharacteristicUUIDB }
        };

        // allow any external to write to the same UI
        StaticLog = (msg) =>
        {
            var line = _idTag + msg;
            Debug.Log(line);
            if (StatusText) StatusText.text = line;
        };

        // only take over when nobody assigned it
        if (BLEBridge.UILog == null)
        {
            BLEBridge.UILog = (s) =>
            {
                var line = _idTag + s;
                if (StatusText) StatusText.text = line;
                Debug.Log(line);
            };
        }
    }

    void Start()
{
    if (_booted) return;
    _booted = true;

    if (StatusText) StatusText.text = _idTag + "[Boot] Scene loaded, waiting permissions...";
#if UNITY_ANDROID && !UNITY_EDITOR
    StartCoroutine(WaitPermsThenInit());
#else
    InitAndScan();
#endif
}

#if UNITY_ANDROID && !UNITY_EDITOR
System.Collections.IEnumerator WaitPermsThenInit()
{
    // 等待权限到位
    string[] need = {
        "android.permission.BLUETOOTH_SCAN",
        "android.permission.BLUETOOTH_CONNECT",
        "android.permission.ACCESS_FINE_LOCATION"
    };
    float timeout = 10f; // 最多等 10 秒（基本够用）
    while (timeout > 0f)
    {
        bool all = true;
        foreach (var p in need)
            all &= UnityEngine.Android.Permission.HasUserAuthorizedPermission(p);

        StatusMessage = $"[Android] Waiting perms... SCAN={UnityEngine.Android.Permission.HasUserAuthorizedPermission(need[0])} CONNECT={UnityEngine.Android.Permission.HasUserAuthorizedPermission(need[1])} LOC={UnityEngine.Android.Permission.HasUserAuthorizedPermission(need[2])}";
        if (all) break;

        // 再次请求（若用户还没操作）
        foreach (var p in need)
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(p))
                UnityEngine.Android.Permission.RequestUserPermission(p);

        timeout -= 1f;
        yield return new WaitForSeconds(1f);
    }

    // 仍未授权就给出明确提示并返回
    bool ok = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN") &&
              UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT") &&
              UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.ACCESS_FINE_LOCATION");

    if (!ok)
    {
        StatusMessage = "[Android] Init aborted: permissions NOT granted. (Open App Permissions in Settings or reinstall and tap ALLOW)";
        yield break;
    }

    // 拿到权限 -> 初始化
    InitAndScan();
}
#endif


    void OnApplicationQuit()
    {
        try { BLEBridge.Quit(); } catch { }
    }

    // ---- permission probe (log only, Android) ----
    void PrintPermissionProbe()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool pScan = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN");
        bool pConn = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT");
        bool pLoc  = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.ACCESS_FINE_LOCATION");
        StatusMessage = $"[Android] Perms: SCAN={pScan} CONNECT={pConn} LOC={pLoc}";
#endif
    }

    // ---------------- init & scan ----------------
    void InitAndScan()
    {
        _state = State.None;
        _scanTimeout = 0f;
        foreach (var b in boards.Values)
        {
            b.Address = null; b.Found = false; b.IsConnected = false;
        }

        string tag =
#if (UNITY_ANDROID && !UNITY_EDITOR)
            "[Android] ";
#elif (UNITY_IOS && !UNITY_EDITOR)
            "[iOS] ";
#else
            "[Editor] ";
#endif
        StatusMessage = tag + "BLE init ...";

        BLEBridge.Initialize(
            () =>
            {
                StatusMessage = tag + "Init OK -> Start Scan";
                StartCoroutine(_DelayedStartScan());
            },
            err => { StatusMessage = tag + "Init FAIL: " + err; }
        );
    }

    private System.Collections.IEnumerator _DelayedStartScan()
    {
        // give system/permission dialog a moment
        yield return new WaitForSeconds(0.2f);
        // don't accidentally double-start
        if (_state == State.None || _state == State.Ready)
            StartScan();
    }

    void StartScan()
{
    _state = State.Scanning;
    _scanTicker = 0f;

    // ✅ 关键：不过滤，返回所有广播。靠 DeviceNameA/B 做 name.Contains 匹配。
    string[] filters = null;

    int found = 0;
    StatusMessage = "[Android] Start Scan (no filter; match by name) ...";

    try
    {
        BLEBridge.ScanForPeripheralsWithServices(
            filters,
            (address, name) =>
            {
                StatusMessage = $"[FOUND#{++found}] {name} ({address})";
                MaybeCaptureDevice(address, name);
            },
            (address, name, rssi, adv) =>
            {
                StatusMessage = $"[FOUND#{++found}] {name} RSSI:{rssi}";
                MaybeCaptureDevice(address, name);
            },
            rssiOnly: false
        );
    }
    catch (Exception ex)
    {
        StatusMessage = "[Android] Scan exception: " + ex.Message;
    }

    _scanTimeout = 25f; // 给足时间
}


    void Update()
    {
        if (_state == State.Scanning)
        {
            _scanTicker += Time.deltaTime;
            if (_scanTicker >= 1f)
            {
                _scanTicker = 0f;
                StatusMessage = $"[Android] ... scanning t={(int)(Time.realtimeSinceStartup)}s";
            }

            if (_scanTimeout > 0f)
            {
                _scanTimeout -= Time.deltaTime;
                if (_scanTimeout <= 0f)
                {
                    StatusMessage = "[Android] Scan timeout, stop and continue with discovered ones.";
                    FinishScanAndReady();
                }
            }
        }
    }

    void MaybeCaptureDevice(string address, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        foreach (var b in boards.Values)
        {
            if (!b.Found && name.Contains(b.DeviceName))
            {
                b.Found = true;
                b.Address = address;
                StatusMessage = $"Lock {b.DeviceName} -> {address}";
                ConnectBoard(b);
            }
        }
    }

    // ---------------- connect ----------------
    void ConnectBoard(Board b)
    {
        if (string.IsNullOrEmpty(b.Address)) return;

        StatusMessage = $"Connecting {b.DeviceName} ...";
        BLEBridge.ConnectToPeripheral(
            b.Address,
            onConnected: _ => { /* optional verbose */ },
            onService: (_, __) => { /* optional verbose */ },
            onCharacteristic: (_, serviceUUID, characteristicUUID) =>
            {
                if (IsEqual(serviceUUID, b.ServiceUUID) && IsEqual(characteristicUUID, b.CharacteristicUUID))
                {
                    b.IsConnected = true;
                    StatusMessage = $"{b.DeviceName} Connected (char confirmed)";
                    if (AllConnected()) FinishScanAndReady();
                }
            }
        );
    }

    void FinishScanAndReady()
    {
        BLEBridge.StopScan();
        _state = State.Ready;
        _scanTimeout = 0f;
        StatusMessage = AllConnected() ? "Both boards connected, READY." : "Partial connected, READY.";
    }

    bool AllConnected()
    {
        foreach (var b in boards.Values) if (!b.IsConnected) return false;
        return true;
    }

    // ---------------- write (collision calls this) ----------------
    public void SendFor(string key, byte value, Action<bool, string> onDone = null)
    {
        key = (key ?? "A").ToUpper();
        if (!boards.ContainsKey(key))
        {
            var msg = $"Unknown board key: {key}";
            StatusMessage = msg;
            onDone?.Invoke(false, msg);
            return;
        }

        var b = boards[key];
        if (!b.IsConnected || string.IsNullOrEmpty(b.Address))
        {
            var msg = $"Board {key} not connected or address empty";
            StatusMessage = msg;
            onDone?.Invoke(false, msg);
            return;
        }

        try
        {
            byte[] data = { value };
            BLEBridge.WriteCharacteristic(
                b.Address, b.ServiceUUID, b.CharacteristicUUID,
                data, data.Length, withResponse: true,
                onWrite: _ =>
                {
                    StatusMessage = $"Write OK -> Board {key}, val=0x{value:X2}";
                    onDone?.Invoke(true, null);
                }
            );
        }
        catch (Exception ex)
        {
            var msg = $"Write EXCEPTION -> Board {key}, val=0x{value:X2}: {ex.Message}";
            StatusMessage = msg;
            onDone?.Invoke(false, msg);
        }
    }

    // ---------------- utils / public accessors ----------------
    bool IsEqual(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    // for triggers/UI: is connected & write-ready
    public bool IsBoardConnected(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        key = key.ToUpper();
        return boards.TryGetValue(key, out var b) && b != null && b.IsConnected && !string.IsNullOrEmpty(b.Address);
    }

    public string GetBoardAddress(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        key = key.ToUpper();
        return boards.TryGetValue(key, out var b) ? b.Address : null;
    }

    public string GetBoardDeviceName(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        key = key.ToUpper();
        return boards.TryGetValue(key, out var b) ? b.DeviceName : null;
    }
}





