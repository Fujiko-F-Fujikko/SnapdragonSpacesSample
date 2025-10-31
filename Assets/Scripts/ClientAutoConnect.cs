using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientAutoConnect : MonoBehaviour
{
  [SerializeField] string serverIp = "192.168.0.10"; // ← サーバPCのLAN IPに変更
  [SerializeField] ushort serverPort = 7777;

  void Awake()
  {
  }

  void Start()
  {
    var nm = NetworkManager.Singleton;
    if (nm == null)
    {
      Debug.LogError("[ClientAutoConnect] NetworkManager not found.");
      return;
    }
    var utp = nm.GetComponent<UnityTransport>();
    if (utp == null) utp = nm.gameObject.AddComponent<UnityTransport>();

    utp.SetConnectionData(serverIp, serverPort);
    if (!nm.StartClient())
    {
      Debug.LogError("[ClientAutoConnect] StartClient failed.");
    }
    else
    {
      Debug.Log($"[ClientAutoConnect] Connecting to {serverIp}:{serverPort} ...");
    }

  }
}
