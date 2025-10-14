using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Netcode HUD（IP/Port編集・常時表示）
/// 接続/切断/サーバ開始終了など、状態変化は必ず Console と HUD に出力する。
/// </summary>
[DefaultExecutionOrder(1000)]
public class NGOHud : MonoBehaviour
{
  string ip;
  ushort port;

  // 直近イベントをHUDに表示（上から新しい順）
  const int MaxLogLines = 8;
  readonly LinkedList<string> _recent = new LinkedList<string>();

  bool _eventsHooked = false;

  NetworkManager Nm => NetworkManager.Singleton;
  UnityTransport TryGetTransport() => Nm ? Nm.GetComponent<UnityTransport>() : null;

  void Awake()
  {
    ip = PlayerPrefs.GetString("NGO_IP", "127.0.0.1");
    port = (ushort)PlayerPrefs.GetInt("NGO_Port", 7777);
  }

  void OnEnable() { TryHookEvents(); }
  void Update() { if (!_eventsHooked) TryHookEvents(); } // 遅延生成対策
  void OnDisable() { UnhookEvents(); }

  void TryHookEvents()
  {
    if (_eventsHooked) return;
    if (Nm == null) return;

    Nm.OnServerStarted += OnServerStarted;
    Nm.OnServerStopped += OnServerStopped;
    Nm.OnClientConnectedCallback += OnClientConnected;
    Nm.OnClientDisconnectCallback += OnClientDisconnected;

    _eventsHooked = true;
    LogHud("HUD ready. Waiting for network events.");
  }

  void UnhookEvents()
  {
    if (!_eventsHooked || Nm == null) return;

    Nm.OnServerStarted -= OnServerStarted;
    Nm.OnServerStopped -= OnServerStopped;
    Nm.OnClientConnectedCallback -= OnClientConnected;
    Nm.OnClientDisconnectCallback -= OnClientDisconnected;

    _eventsHooked = false;
  }

  // ===== Netcode Events =====
  void OnServerStarted()
  {
    if (Nm.IsHost) LogHud("Server started (Host mode).");
    else LogHud("Server started.");
  }

  void OnServerStopped(bool _)
  {
    LogHud("Server stopped.");
  }

  void OnClientConnected(ulong clientId)
  {
    bool isLocal = Nm && clientId == Nm.LocalClientId && Nm.IsClient;
    LogHud(isLocal
        ? $"Client CONNECTED (localId:{clientId})."
        : $"Client CONNECTED (remoteId:{clientId}).");
  }

  void OnClientDisconnected(ulong clientId)
  {
    bool isLocal = Nm && clientId == Nm.LocalClientId && Nm.IsClient;
    // 可能なら切断理由も併記（空の場合あり）
    string reason = (Nm != null && Nm.DisconnectReason.Length > 0) ? $" reason='{Nm.DisconnectReason}'" : "";
    LogHud(isLocal
        ? $"Client DISCONNECTED (localId:{clientId}){reason}."
        : $"Client DISCONNECTED (remoteId:{clientId}){reason}.");
  }

  // ===== UI =====
  void OnGUI()
  {
    const int w = 320;
    GUILayout.BeginArea(new Rect(10, 10, w, 280), GUI.skin.box);

    var nm = Nm;
    var ut = TryGetTransport();

    // --- ステータス行 ---
    string status = "OFFLINE";
    if (nm != null)
    {
      if (nm.IsHost) status = $"HOST  (localId:{nm.LocalClientId})";
      else if (nm.IsServer) status = "SERVER (listening)";
      else if (nm.IsClient) status = nm.IsConnectedClient
                                      ? $"CLIENT (connected) localId:{nm.LocalClientId}"
                                      : "CLIENT (connecting...)";
    }
    GUILayout.Label($"Status: {status}");

    GUILayout.Space(6);
    DrawAddressEditors();

    GUILayout.Space(6);
    if (nm == null)
    {
      GUI.enabled = false;
      GUILayout.Button("Start Host");
      GUILayout.Button("Start Client");
      GUILayout.Button("Start Server");
      GUI.enabled = true;
      GUILayout.Label("⚠ NetworkManager not found in scene.");
    }
    else if (!nm.IsListening)
    {
      bool canStart = ut != null;
      if (!canStart) GUI.enabled = false;

      if (GUILayout.Button("Start Host"))
      {
        ApplyConnection(isServer: true);
        bool ok = nm.StartHost();
        LogHud(ok ? "StartHost() OK." : "StartHost() FAILED.");
      }
      if (GUILayout.Button("Start Client"))
      {
        ApplyConnection(isServer: false);
        bool ok = nm.StartClient();
        LogHud(ok ? "StartClient() OK. Connecting..." : "StartClient() FAILED.");
      }
      if (GUILayout.Button("Start Server"))
      {
        ApplyConnection(isServer: true);
        bool ok = nm.StartServer();
        LogHud(ok ? "StartServer() OK." : "StartServer() FAILED.");
      }

      if (!canStart)
      {
        GUI.enabled = true;
        GUILayout.Label("⚠ UnityTransport not found on NetworkManager.");
      }
    }
    else
    {
      if (GUILayout.Button("Shutdown"))
      {
        LogHud("Shutdown requested.");
        nm.Shutdown();
      }
    }

    GUILayout.Space(8);
    GUILayout.Label("Recent events:");
    GUILayout.BeginVertical(GUI.skin.textArea, GUILayout.Height(80));
    foreach (var line in _recent)
      GUILayout.Label(line);
    GUILayout.EndVertical();

    GUILayout.EndArea();
  }

  void DrawAddressEditors()
  {
    GUILayout.Label($"Addr: {ip}:{port}");
    GUILayout.BeginHorizontal();
    GUILayout.Label("IP", GUILayout.Width(25));
    ip = GUILayout.TextField(ip, GUILayout.Width(150));
    GUILayout.Label("Port", GUILayout.Width(35));
    var portStr = GUILayout.TextField(port.ToString(), GUILayout.Width(60));
    if (ushort.TryParse(portStr, out var p)) port = p;
    GUILayout.EndHorizontal();
  }

  void ApplyConnection(bool isServer)
  {
    PlayerPrefs.SetString("NGO_IP", ip);
    PlayerPrefs.SetInt("NGO_Port", port);
    PlayerPrefs.Save();

    var ut = TryGetTransport();
    if (ut == null)
    {
      LogHud("ERROR: UnityTransport not found.");
      return;
    }

    if (isServer) ut.SetConnectionData("0.0.0.0", port); // 全NIC待ち受け
    else ut.SetConnectionData(ip, port);        // サーバ宛

    LogHud($"Set connection: {(isServer ? "Server listen 0.0.0.0" : $"Client to {ip}")}:{port}");
  }

  // ===== ログ/HUD共通出力 =====
  void LogHud(string msg)
  {
    Debug.Log("[NGOHud] " + msg);

    _recent.AddFirst("• " + msg);
    while (_recent.Count > MaxLogLines)
      _recent.RemoveLast();
  }
}
