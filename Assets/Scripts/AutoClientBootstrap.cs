using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// 起動後すぐにClientモードでサーバへ接続するブートストラップ。
/// - Server IP / Port は Inspector で設定。
/// - Awake時点ではまだNetcode ILPP初期化が終わっていないため、
///   数フレーム待ってから StartClient() を呼ぶ。
/// - 接続状態をGUIとログで表示。
/// </summary>
[DefaultExecutionOrder(1000)]
public class AutoClientBootstrap : MonoBehaviour
{
  [Header("Server Endpoint (設定してビルド)")]
  [Tooltip("サーバーのIPv4アドレス (例: 192.168.19.192)")]
  public string serverIp = "192.168.0.2";

  [Tooltip("接続ポート (UnityTransport側と一致させる)")]
  public ushort port = 7777;

  [Header("接続オプション")]
  [Tooltip("切断時に自動で再接続する")]
  public bool autoReconnect = true;

  [Tooltip("再接続までの待機秒数")]
  public float retryIntervalSeconds = 3f;

  [Header("HUD")]
  [Tooltip("左上に状態を表示")]
  public bool showHud = true;

  private string _status = "BOOT";
  private bool _trying = false;
  private UnityTransport _transport;
  private NetworkManager _nm;

  void Awake()
  {
    _nm = NetworkManager.Singleton;
    if (_nm == null)
    {
      Debug.LogError("[AutoClientBootstrap] NetworkManager not found in scene!");
      enabled = false;
      return;
    }

    _transport = _nm.GetComponent<UnityTransport>();
    if (_transport == null)
    {
      Debug.LogError("[AutoClientBootstrap] UnityTransport not found on NetworkManager!");
      enabled = false;
      return;
    }

    // コールバック登録
    _nm.OnClientConnectedCallback += OnClientConnected;
    _nm.OnClientDisconnectCallback += OnClientDisconnected;
  }

  void Start()
  {
    // 起動直後に接続を開始（ILPP初期化を待ってから）
    StartCoroutine(DelayedConnectRoutine());
  }

  private IEnumerator DelayedConnectRoutine()
  {
    _status = "WAITING ILPP INIT...";
    // IL Post Processor が完了するまで1～2フレーム待機
    yield return null;
    yield return null;

    // 念のためもう少し遅延
    yield return new WaitForSeconds(0.3f);

    // UnityTransportにサーバ情報を設定
    _transport.SetConnectionData(serverIp, port);
    Log($"Set endpoint {serverIp}:{port}");

    yield return new WaitForSeconds(0.2f); // ネットワーク層の初期化安定待ち

    TryStartClient();
  }

  private void TryStartClient()
  {
    if (_trying) return;
    _trying = true;
    _status = "CLIENT (connecting...)";

    try
    {
      bool ok = _nm.StartClient();
      if (ok)
      {
        Log("StartClient() OK. Connecting...");
      }
      else
      {
        _trying = false;
        _status = "CLIENT (start failed)";
        Log("StartClient() FAILED.");
        if (autoReconnect) Invoke(nameof(ScheduleRetry), retryIntervalSeconds);
      }
    }
    catch (System.Exception ex)
    {
      _trying = false;
      _status = "CLIENT (exception)";
      Debug.LogError("[AutoClientBootstrap] Exception while starting client: " + ex);
      if (autoReconnect) Invoke(nameof(ScheduleRetry), retryIntervalSeconds);
    }
  }

  private void ScheduleRetry()
  {
    if (!autoReconnect) return;
    Log($"Retry in {retryIntervalSeconds:0.0}s...");
    Invoke(nameof(TryStartClient), retryIntervalSeconds);
  }

  private void OnClientConnected(ulong clientId)
  {
    _trying = false;
    _status = $"CLIENT (connected) localId:{_nm.LocalClientId}";
    Log($"CONNECTED. localId={_nm.LocalClientId}");
  }

  private void OnClientDisconnected(ulong clientId)
  {
    bool isLocal = _nm.IsClient && clientId == _nm.LocalClientId;
    string reason = (_nm.DisconnectReason?.Length ?? 0) > 0 ? $" reason='{_nm.DisconnectReason}'" : "";
    _status = isLocal ? $"CLIENT (disconnected){reason}" : $"REMOTE DISCONNECT {clientId}{reason}";
    Log($"DISCONNECTED. {reason}");
    _trying = false;

    if (autoReconnect) ScheduleRetry();
  }

  void OnGUI()
  {
    if (!showHud) return;
    const int w = 380;
    GUILayout.BeginArea(new Rect(10, 10, w, 80), GUI.skin.box);
    GUILayout.Label($"Status: {_status}");
    GUILayout.Label($"Target: {serverIp}:{port}");
    GUILayout.EndArea();
  }

  private void Log(string msg)
  {
    Debug.Log($"[AutoClientBootstrap] {msg}");
  }
}
