// BleAndroidPermissionBoot.cs（保持不变）
using UnityEngine;

public class BleAndroidPermissionBoot : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    void Awake()
    {
        string[] perms = {
            "android.permission.BLUETOOTH_SCAN",
            "android.permission.BLUETOOTH_CONNECT",
            "android.permission.ACCESS_FINE_LOCATION"
        };
        foreach (var p in perms)
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(p))
                UnityEngine.Android.Permission.RequestUserPermission(p);
    }
#endif
}



