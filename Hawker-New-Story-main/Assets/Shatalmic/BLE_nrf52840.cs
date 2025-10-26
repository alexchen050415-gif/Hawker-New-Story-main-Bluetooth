using UnityEngine;
using UnityEngine.UI;

public class BLE_nrf52840 : MonoBehaviour
{
    public string DeviceName = "ledbtn";
    public string ServiceUUID = "A9E90000-194C-4523-A473-5FDF36AA4D20";
    public string CharactristicUUID = "A9E90001-194C-4523-A473-5FDF36AA4D20";
    //public string ButtonUUID = "A9E90002-194C-4523-A473-5FDF36AA4D20";

    enum States
    {
        None,
        Scan,
        ScanRSSI,
        ReadRSSI,
        Connect,
        RequestMTU,
        Subscribe,
        Unsubscribe,
        Disconnect,
    }

    private bool _connected = false;
    private float _timeout = 0f;
    private States _state = States.None;
    private string _deviceAddress;
    private bool _rssiOnly = false;
    private int _rssi = 0;

    public Text StatusText;
    public Text ButtonPositionText;

    private string StatusMessage
    {
        set
        {
            BluetoothLEHardwareInterface.Log(value);
            StatusText.text = value;
        }
    }

    void Reset()
    {
        _connected = false;
        _timeout = 0f;
        _state = States.None;
        _deviceAddress = null;
        _rssi = 0;
    }

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    void StartProcess()
    {
        Reset();
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {

            SetState(States.Scan, 0.1f);

        }, (error) =>
        {

            StatusMessage = "Error during initialize: " + error;
        });
    }

    // Use this for initialization
    void Start()
    {
        StartProcess();
    }

    private void ProcessButton(byte[] bytes)
    {
        if (bytes[0] == 0x00)
            ButtonPositionText.text = "Not Pushed";
        else
            ButtonPositionText.text = "Pushed";
    }

    // Update is called once per frame
    void Update()
    {
        if (_timeout > 0f)
        {
            _timeout -= Time.deltaTime;
            if (_timeout <= 0f)
            {
                _timeout = 0f;

                switch (_state)
                {
                    case States.None:
                        break;

                    case States.Scan:
                        StatusMessage = "Scanning for " + DeviceName;

                        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
                        {
                            // if your device does not advertise the rssi and manufacturer specific data
                            // then you must use this callback because the next callback only gets called
                            // if you have manufacturer specific data

                            if (!_rssiOnly)
                            {
                                if (name.Contains(DeviceName))
                                {
                                    StatusMessage = "Found " + name;

                                    // found a device with the name we want
                                    // this example does not deal with finding more than one
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, (address, name, rssi, bytes) =>
                        {

                            if (name.Contains(DeviceName))
                            {
                                StatusMessage = "Found " + name;

                                if (_rssiOnly)
                                {
                                    _rssi = rssi;
                                }
                                else
                                {
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, _rssiOnly); 

                        if (_rssiOnly)
                            SetState(States.ScanRSSI, 0.5f);
                        break;

                    case States.ScanRSSI:
                        break;

                    case States.ReadRSSI:
                        StatusMessage = $"Call Read RSSI";
                        BluetoothLEHardwareInterface.ReadRSSI(_deviceAddress, (address, rssi) =>
                        {
                            StatusMessage = $"Read RSSI: {rssi}";
                        });

                        SetState(States.ReadRSSI, 2f);
                        break;

                    case States.Connect:
                        StatusMessage = "Connecting...";
                        BluetoothLEHardwareInterface.ConnectToPeripheral(_deviceAddress, null, null, (address, serviceUUID, characteristicUUID) =>
                        {
                            StatusMessage = serviceUUID;//"Connected...";

                            BluetoothLEHardwareInterface.StopScan();

                            if (IsEqual(serviceUUID, ServiceUUID))
                            {
                                StatusMessage = "Connected!";
                            }
                        });
                        break;

                    case States.RequestMTU:
                        StatusMessage = "Requesting MTU";

                        BluetoothLEHardwareInterface.RequestMtu(_deviceAddress, 185, (address, newMTU) =>
                        {
                            StatusMessage = "MTU set to " + newMTU.ToString();

                            SetState(States.Subscribe, 0.1f);
                        });
                        break;

                    case States.Disconnect:
                        StatusMessage = "Commanded disconnect.";

                        if (_connected)
                        {
                            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
                            {
                                StatusMessage = "Device disconnected";
                                BluetoothLEHardwareInterface.DeInitialize(() =>
                                {
                                    _connected = false;
                                    _state = States.None;
                                });
                            });
                        }
                        else
                        {
                            BluetoothLEHardwareInterface.DeInitialize(() =>
                            {
                                _state = States.None;
                            });
                        }
                        break;
                }
            }
        }
    }

    private bool ledON = false;
    public void OnLED()
    {
        ledON = !ledON;
        if (ledON)
        {
            SendByte((byte)0x01);
        }
        else
        {
            SendByte((byte)0x00);
        }
    }

    public void Humi_begin()
    {
        SendByte((byte)0);
    }
    public void Humi_end()
    {
        SendByte((byte)1);
    }
    public void Humi_1()
    {
        SendByte((byte)2);
    }
    public void Humi_2()
    {
        SendByte((byte)3);
    }
    public void Humi_3()
    {
        SendByte((byte)4);
    }
    public void Humi_4()
    {
        SendByte((byte)5);
    }

    string FullUUID(string uuid)
    {
        string fullUUID = uuid;
        if (fullUUID.Length == 4)
            fullUUID = "0000" + uuid + "-0000-1000-8000-00805f9b34fb";

        return fullUUID;
    }

    bool IsEqual(string uuid1, string uuid2)
    {
        return (uuid1.ToUpper().Equals(uuid2.ToUpper()));
    }

    void SendByte(byte value)
    {
        byte[] data = { value };
        BluetoothLEHardwareInterface.WriteCharacteristic(_deviceAddress, ServiceUUID, CharactristicUUID, data, data.Length, true, (characteristicUUID) =>
        {

            BluetoothLEHardwareInterface.Log("Write Succeeded");
        });
    }
}
