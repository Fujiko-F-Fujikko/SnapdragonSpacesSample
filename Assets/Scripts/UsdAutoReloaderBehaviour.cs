// --- USDパッケージがあるビルド（Editor/Serverなど）向け本体 ---
#if HAS_UNITY_USD
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Formats.USD;
using Unity.Netcode;
using UnityEngine;



[DisallowMultipleComponent]
public class UsdAutoReloaderBehaviour : MonoBehaviour
{
  [Header("Refs")]
  [Tooltip("同じGameObjectに付いている UsdAsset。未指定なら自動取得します")]
  [SerializeField] private UsdAsset usdAsset;

  [Header("Options")]
  [Tooltip("NGO使用時、Server/Host のときだけ動かす")]
  [SerializeField] private bool onlyIfServer = true;

  [Tooltip("更新チェック間隔（秒）")]
  [SerializeField] private float pollInterval = 0.5f;

  [Tooltip("連続保存をまとめる待ち時間（秒）")]
  [SerializeField] private float debounceSeconds = 1.0f;

  [SerializeField] private bool verboseLog = false;

  // ▼ 追加: リロード後に子GameObjectを自動でネットワーク化するか
  [Header("Networkize")]
  [Tooltip("リロードしたUSDが生成するGameObjectに NetworkObject を自動付与して Spawn する")]
  [SerializeField] private bool autoNetworkize = true;

  // ▼ 追加: ダミーのNetwork Object
  [SerializeField] private GameObject dummyNetworkObjectPrefab;

  // ▼ 追加: どのTransform以下を対象にするか（未指定ならこのGameObject以下）
  [SerializeField] private Transform networkizeRoot;

  private readonly List<NetworkObject> _spawnedDummies = new();


  string _path;
  DateTime _lastWriteUtc;
  float _nextPollAt, _scheduledAt = -1f;

  void Start()
  {
    if (!usdAsset) usdAsset = GetComponent<UsdAsset>();
    if (!usdAsset)
    {
      Debug.LogWarning("[USD] UsdAsset が見つかりません。", this);
      enabled = false; return;
    }

    if (NetworkManager.Singleton == null)
    {
      Debug.Log("[USD] UsdAutoReloaderBehaviour disabled (no NetworkManager).", this);
      enabled = false; return;
    }
    if (onlyIfServer && !NetworkManager.Singleton.IsServer)
    {
      Debug.Log("[USD] UsdAutoReloaderBehaviour disabled (not server).", this);
      enabled = false; return;
    }

    _path = usdAsset.usdFullPath;
    if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
    {
      Debug.LogWarning($"[USD] 監視対象ファイルが無効です: {_path}", this);
      enabled = false; return;
    }
    _path = Path.GetFullPath(_path);
    _lastWriteUtc = File.GetLastWriteTimeUtc(_path);
    _nextPollAt = Time.unscaledTime + pollInterval;
    if (verboseLog) Debug.Log($"[USD] Watching: {_path}", this);

    if (!networkizeRoot) networkizeRoot = this.transform;
  }

  void Update()
  {
    var now = Time.unscaledTime;

    if (now >= _nextPollAt)
    {
      _nextPollAt = now + pollInterval;
      try
      {
        var wt = File.GetLastWriteTimeUtc(_path);
        if (wt > _lastWriteUtc)
        {
          _lastWriteUtc = wt;
          _scheduledAt = now + debounceSeconds;  // デバウンス
          if (verboseLog) Debug.Log("[USD] Change detected, scheduled reload.", this);
        }
      }
      catch (Exception e)
      {
        if (verboseLog) Debug.LogWarning($"[USD] Watch error: {e.Message}", this);
      }
    }

    if (_scheduledAt > 0 && now >= _scheduledAt)
    {
      _scheduledAt = -1;
      TryReload();
    }
  }

  public void TryReload()
  {
    // ★前回のダミーを片付ける
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
    {
      DespawnAllDummies();
    }

    if (!usdAsset) return;
    try
    {
      // USDをその場で更新
      usdAsset.Reload(true);   // ★ “その場更新” (NetworkObjectを壊さない)
      if (verboseLog) Debug.Log("[USD] Reload(true) executed.", this);

      // ▼ 追加: 更新後にネットワーク化してSpawnする
      if (autoNetworkize)
      {
        NetworkizeSpawnCreatedObjects();
      }
    }
    catch (Exception ex)
    {
      Debug.LogException(ex, this);
    }
  }

  /// <summary>
  /// USDの再ロードで生成/更新されたGameObjectに NetworkObject を付けて Spawn する
  /// </summary>
  // UsdAutoReloaderBehaviour 側
  void NetworkizeSpawnCreatedObjects()
  {
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
      return;

    var root = networkizeRoot ? networkizeRoot : this.transform;

    foreach (var tr in root.GetComponentsInChildren<Transform>(true))
    {
      if (tr == this.transform) continue;
      if (!tr.GetComponent<MeshRenderer>() && !tr.GetComponent<Camera>()) continue;

      var go = Instantiate(dummyNetworkObjectPrefab);
      var no = go.GetComponent<NetworkObject>();
      var view = go.GetComponent<UsdDummyView>();

      no.Spawn();

      tr.gameObject.SetActive(false); // USDファイルから読み込んだそのものは見せない

      Color col = ExtractColorFromUsdNode(tr);

      // USD側のパスは今まで通り
      string usdPath = tr.GetComponent<UsdPrimSource>().m_usdPrimPath;

      Debug.Log("tr name:" + tr.gameObject.name + " usdPath:" + usdPath);

      // ★ ここで種類を決める
      byte kind = GuessVisualKind(usdPath, tr);

      view.ServerInit(
          usdPath,
          tr.position,
          tr.rotation,
          tr.lossyScale,
          col,
          kind
      );

      // ★ Spawnしたら覚えておく
      _spawnedDummies.Add(no);
    }
  }

  byte GuessVisualKind(string usdPath, Transform tr)
  {
    // 例1: パスで判定
    if (usdPath.Contains("Cube"))
      return 0; // Cube
    if (usdPath.Contains("Sphere"))
      return 1; // Sphere
    if (usdPath.Contains("Cylinder"))
      return 2; // Cylinder

    if (usdPath.Contains("Camera"))
      return 3; // Camera

    // 例2: USD側で "SM_" を付けておいて、それをカスタムMeshにする
    if (usdPath.Contains("Spoon"))
      return 10;   // customMesh0
    if (usdPath.Contains("Crayon"))
      return 11;   // customMesh1
    if (usdPath.Contains("Chair"))
      return 12;   // customMesh2
    if (usdPath.Contains("CafeTableParasol"))
      return 13;   // customMesh3
    if (usdPath.Contains("CafeTableSet"))
      return 14;   // customMesh4
    if (usdPath.Contains("Coffee"))
      return 15;   // customMesh5
    if (usdPath.Contains("Laptop"))
      return 16;   // customMesh6
    if (usdPath.Contains("Plant_01"))
      return 17;   // customMesh7
    if (usdPath.Contains("Plant_02"))
      return 18;   // customMesh8
    if (usdPath.Contains("Plant_03"))
      return 19;   // customMesh9
    if (usdPath.Contains("UFO"))
      return 20;   // customMesh10

    // 何も当たらなければCube
    return 0;
  }

  Color ExtractColorFromUsdNode(Transform tr)
  {
    var mr = tr.GetComponentInChildren<MeshRenderer>();
    if (mr && mr.sharedMaterial && mr.sharedMaterial.HasProperty("_Color"))
    {
      return mr.sharedMaterial.color;
    }
    return Color.white;
  }

  void DespawnAllDummies()
  {
    // サーバーだけがDespawnする
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
      return;

    foreach (var no in _spawnedDummies)
    {
      if (no == null) continue;
      // true を渡すとクライアント側でもGameObjectごと消える
      no.Despawn(true);
    }

    _spawnedDummies.Clear();
  }

}
#else
// --- USDパッケージが無いビルド（クライアント等）でもコンパイルが通るダミー ---
using UnityEngine;
public class UsdAutoReloaderBehaviour : MonoBehaviour
{
    [SerializeField] private bool logOnce = true;
    void Awake() { if (logOnce) Debug.Log("[USD] UsdAutoReloaderBehaviour disabled (HAS_UNITY_USD not defined).", this); }
}
#endif
