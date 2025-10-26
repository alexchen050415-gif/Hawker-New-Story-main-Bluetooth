using UnityEngine;

/// <summary>
/// 进入头部触发器 → 立即上报“发往哪个板、发了哪个码、当时该板是否已连接”；
/// 离开 → 上报 offCode；仍会实际发送，但不等待回调。
/// </summary>
public class PizzelTriggerPizzel : MonoBehaviour
{
    [Header("必填：BLE 管理器")]
    public BLE_nrf52840Dual ble;

    [Header("必填：状态面板管理器")]
    public PizzelStatusManager statusManager;

    [Header("配置：这是谁的喷嘴？")]
    public string boardKey = "A";      // "A" or "B"
    [Range(1,4)] public int channel = 1;

    [Header("触发过滤")]
    public string headTag = "Head";

    [Header("协议：离开时发送的关闭码")]
    public byte offCode = 0;

    [Header("可选：防抖（秒）")]
    [Range(0f, 0.5f)] public float debounceSeconds = 0.05f;

    private float _nextAllowedTime = 0f;
    private string _pizzelId; // "A1".."B4"

    void Awake() => _pizzelId = boardKey.ToUpper() + channel.ToString();

    bool DebounceOK()
    {
        if (Time.time < _nextAllowedTime) return false;
        _nextAllowedTime = Time.time + debounceSeconds;
        return true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(headTag) || !DebounceOK()) return;

        byte payload = (byte)Mathf.Clamp(channel, 1, 4);
        string key = boardKey.ToUpper();

        // 读取 BLE 管理器的实时连接状态
        bool linkReady = (ble != null) && ble.IsBoardConnected(key);

        // 1) 立即上报（包含目标板 & 是否连接）
        if (statusManager != null)
            statusManager.ReportSent(_pizzelId, key, payload, linkReady);

        // 2) 继续真正发送（回调忽略）
        if (ble != null) ble.SendFor(key, payload, null);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(headTag) || !DebounceOK()) return;

        string key = boardKey.ToUpper();
        bool linkReady = (ble != null) && ble.IsBoardConnected(key);

        if (statusManager != null)
            statusManager.ReportSent(_pizzelId, key, offCode, linkReady);

        if (ble != null) ble.SendFor(key, offCode, null);
    }
}

