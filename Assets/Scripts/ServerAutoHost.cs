using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[DefaultExecutionOrder(1000)] // NetworkManager(Awake) の後に動くよう遅らせる
[DisallowMultipleComponent]
public class ServerAutoHost : MonoBehaviour
{
  [SerializeField] ushort listenPort = 7777;
  [SerializeField] bool startAsHost = true; // false なら Server のみ
  [SerializeField] float timeoutSec = 5f;

  NetworkManager _nm;

  void Awake()
  {
    // ここでは起動しない（順序問題を避ける）
  }

  void Start()
  {
    StartCoroutine(StartWhenReady());
  }

  IEnumerator StartWhenReady()
  {
    // 1) 同じGOにある NetworkManager を優先
    _nm = GetComponent<NetworkManager>();

    // 2) なければシーン全体から探す（無効オブジェクトも含む）
    if (_nm == null)
    {
      var all = FindObjectsOfType<NetworkManager>(true);
      if (all.Length > 1)
      {
        Debug.LogWarning($"[ServerAutoHost] Multiple NetworkManagers found: {string.Join(", ", all.Select(a => a.name))}");
      }
      _nm = all.FirstOrDefault();
    }

    // 3) 見つかっても無効なら有効化
    if (_nm != null && !_nm.gameObject.activeInHierarchy)
    {
      Debug.LogWarning($"[ServerAutoHost] NetworkManager found but inactive. Activating: {_nm.name}");
      _nm.gameObject.SetActive(true);
    }

    // 4) 一定時間待ってもダメならエラー
    float t = 0f;
    while ((_nm == null) && t < timeoutSec)
    {
      yield return null;
      t += Time.unscaledDeltaTime;
      // 途中で生成された場合に対応
      _nm = NetworkManager.Singleton ?? FindObjectOfType<NetworkManager>(true);
    }

    if (_nm == null)
    {
      Debug.LogError("[ServerAutoHost] NetworkManager not found (even after waiting).");
      yield break;
    }

    // Transport を用意
    var utp = _nm.GetComponent<UnityTransport>();
    if (utp == null) utp = _nm.gameObject.AddComponent<UnityTransport>();

    // Listen 設定（0.0.0.0 で全IF待受）
    utp.SetConnectionData("0.0.0.0", listenPort, "0.0.0.0");

    // 既に起動済みなら何もしない
    if (_nm.IsServer || _nm.IsClient)
    {
      Debug.Log("[ServerAutoHost] NetworkManager already started. Skip.");
      yield break;
    }

    // 起動
    bool ok = startAsHost ? _nm.StartHost() : _nm.StartServer();
    if (!ok)
    {
      Debug.LogError("[ServerAutoHost] Failed to start host/server.");
      yield break;
    }

    Debug.Log($"[ServerAutoHost] {(startAsHost ? "Host" : "Server")} started. Port={listenPort}");
    Debug.Log($"[ServerAutoHost] LAN IPs: {string.Join(", ", GetLocalIPv4())}");
  }

  static string[] GetLocalIPv4()
  {
    try
    {
      return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToArray();
    }
    catch { return new[] { "(unknown)" }; }
  }
}
