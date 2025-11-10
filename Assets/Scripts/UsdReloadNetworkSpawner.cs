// USD再ロード後に生成されたGameObjectを NetworkObject 化して Spawn する専用コンポーネント
#if HAS_UNITY_USD
using System.Collections.Generic;
using Unity.Formats.USD;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UsdAutoReloaderBehaviour))]
public class UsdReloadNetworkSpawner : MonoBehaviour
{
  [Header("Spawn Options")]
  [Tooltip("Spawnableとして扱うオブジェクトのキーワードリスト。")]
  [SerializeField] private List<string> spawnableObjectKeywordList = new();

  [Tooltip("ダミー表示用の NetworkObject プレハブ。UsdDummyView + NetworkObject を持っている想定")]
  [SerializeField] private GameObject dummyNetworkObjectPrefab;

  [Tooltip("どのTransform以下を走査するか。未指定ならこのGameObject以下")]
  [SerializeField] private Transform networkizeRoot;

  [Tooltip("Server/Host のときだけ Spawn を行う")]
  [SerializeField] private bool onlyIfServer = true;

  [SerializeField] private bool verboseLog = false;

  private UsdAutoReloaderBehaviour _reloader;
  private readonly List<NetworkObject> _spawnedDummies = new();

  void Awake()
  {
    _reloader = GetComponent<UsdAutoReloaderBehaviour>();
    if (!networkizeRoot) networkizeRoot = this.transform;
  }

  void OnEnable()
  {
    if (_reloader != null)
      _reloader.OnUsdReloaded += OnUsdReloaded;
  }

  void OnDisable()
  {
    if (_reloader != null)
      _reloader.OnUsdReloaded -= OnUsdReloaded;
  }

  public void ForceRespawn()
  {
    OnUsdReloaded(null);
  }

  private void OnUsdReloaded(UsdAsset asset)
  {
    if (onlyIfServer && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer))
    {
      if (verboseLog) Debug.Log("[UsdReloadNetworkSpawner] Skip spawn (not server).", this);
      return;
    }
    DespawnAll();
    SpawnFromUsd();
  }

  private void SpawnFromUsd()
  {
    if (dummyNetworkObjectPrefab == null)
    {
      if (verboseLog) Debug.LogWarning("[UsdReloadNetworkSpawner] dummyNetworkObjectPrefab が設定されていません", this);
      return;
    }

    var root = networkizeRoot ? networkizeRoot : this.transform;
    foreach (var tr in root.GetComponentsInChildren<Transform>(true))
    {
      if (tr == this.transform) continue; // 自分自身はスキップ
      if (!tr.GetComponent<MeshRenderer>() && !tr.GetComponent<Camera>()) continue; // MeshRenderer か Camera が無いものはスキップ

      // USDオリジナルは非表示化
      tr.gameObject.SetActive(false);

      var go = Instantiate(dummyNetworkObjectPrefab);
      var no = go.GetComponent<NetworkObject>();
      var view = go.GetComponent<UsdDummyView>();
      if (no == null || view == null)
      {
        Debug.LogWarning("[UsdReloadNetworkSpawner] Prefab に NetworkObject / UsdDummyView がありません", dummyNetworkObjectPrefab);
        Destroy(go);
        continue;
      }

      no.Spawn();

      Color col = ExtractColor(tr);
      string usdPath = ExtractUsdPath(tr);
      byte kind = GuessVisualKind(usdPath);

      view.ServerInit(
          usdPath,
          tr.position,
          tr.rotation,
          tr.lossyScale,
          col,
          kind
      );

      _spawnedDummies.Add(no);

      if (verboseLog)
      {
        Debug.Log($"[UsdReloadNetworkSpawner] Spawn dummy kind={kind} path={usdPath}", go);
      }
    }
  }

  private string ExtractUsdPath(Transform tr)
  {
    var prim = tr.GetComponent<UsdPrimSource>();
    return prim != null ? prim.m_usdPrimPath : tr.gameObject.name;
  }

  private Color ExtractColor(Transform tr)
  {
    var mr = tr.GetComponentInChildren<MeshRenderer>();
    if (mr && mr.sharedMaterial && mr.sharedMaterial.HasProperty("_Color"))
    {
      return mr.sharedMaterial.color;
    }
    return Color.white;
  }

  private byte GuessVisualKind(string usdPath)
  {
    if (string.IsNullOrEmpty(usdPath)) return 0;
    // 既定オブジェクト
    if (usdPath.Contains("Cube")) return 0;
    if (usdPath.Contains("Sphere")) return 1;
    if (usdPath.Contains("Cylinder")) return 2;
    if (usdPath.Contains("Camera")) return 3;

    for (int i = 0; i < spawnableObjectKeywordList.Count; i++)
    {
      if (usdPath.Contains(spawnableObjectKeywordList[i]))
        return (byte)(10 + i);
    }
    return 0;
  }

  private void DespawnAll()
  {
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
      return;
    foreach (var no in _spawnedDummies)
    {
      if (no == null) continue;
      no.Despawn(true);
    }
    _spawnedDummies.Clear();
  }
}
#else
using UnityEngine;
public class UsdReloadNetworkSpawner : MonoBehaviour
{
  [SerializeField] private bool logOnce = true;
  void Awake() { if (logOnce) Debug.Log("[UsdReloadNetworkSpawner] UsdReloadNetworkSpawner disabled (HAS_UNITY_USD not defined).", this); }
}
#endif
