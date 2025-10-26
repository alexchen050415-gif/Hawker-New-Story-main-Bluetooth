using UnityEngine;
using UnityEngine.Android;
using System.Collections;
using System.Collections.Generic;

public class BLEPermissionManager : MonoBehaviour
{
    private readonly List<string> permissionsToRequest = new List<string>();

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CheckAndRequestPermissions();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void CheckAndRequestPermissions()
    {
        // For Android 6.0 (API 23) and above
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            permissionsToRequest.Add(Permission.FineLocation);
        }

        // For Android 12 (API 31) and above
        if (AndroidVersion() >= 31)
        {
            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
            {
                permissionsToRequest.Add("android.permission.BLUETOOTH_SCAN");
            }

            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
            {
                permissionsToRequest.Add("android.permission.BLUETOOTH_CONNECT");
            }

            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_ADVERTISE"))
            {
                permissionsToRequest.Add("android.permission.BLUETOOTH_ADVERTISE");
            }
        }

        if (permissionsToRequest.Count > 0)
        {
            Permission.RequestUserPermissions(permissionsToRequest.ToArray());
        }
        else
        {
            Debug.Log("All required permissions already granted.");
        }
    }

    int AndroidVersion()
    {
        using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return buildVersion.GetStatic<int>("SDK_INT");
        }
    }
#endif
}
