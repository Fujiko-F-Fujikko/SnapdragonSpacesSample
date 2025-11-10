#if HAS_UNITY_USD
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NGOSpawner : NetworkBehaviour
{
  [System.Serializable]
  public struct SpawnEntry
  {
    public NetworkObject Prefab;    // SpawnするNetworkObjectプレハブ
    public Vector3 initialPosition;        // 出現位置
    public Vector3 initialRotation;        // Euler角度
    public Vector3 initialScale;           // ローカルスケール (初期値は OnValidate で 1,1,1 に補正)
  }

  [SerializeField] private List<SpawnEntry> spawnEntries = new(); // 複数同時スポーン用リスト

  // インスペクタで新規追加した要素の initialScale が (0,0,0) のままなら (1,1,1) に自動補正する。
  private void OnValidate()
  {
    if (spawnEntries == null) return;
    for (int i = 0; i < spawnEntries.Count; i++)
    {
      var entry = spawnEntries[i];
      if (entry.initialScale == Vector3.zero)
      {
        entry.initialScale = Vector3.one;
        spawnEntries[i] = entry; // struct を再代入して反映
      }
    }
  }

  public override void OnNetworkSpawn()
  {
    if (!IsServer) return;

    if (spawnEntries == null || spawnEntries.Count == 0)
    {
      Debug.LogWarning("[NGOSpawner] NGOSpawner: spawnEntries が空のため何もスポーンしません。", this);
      return;
    }

    foreach (var entry in spawnEntries)
    {
      if (entry.Prefab == null)
      {
        Debug.LogWarning("[NGOSpawner] NGOSpawner: Prefab が null のエントリをスキップします。", this);
        continue;
      }

      var obj = Instantiate(entry.Prefab, entry.initialPosition, Quaternion.Euler(entry.initialRotation));
      obj.transform.localScale = entry.initialScale;
      obj.Spawn(true); // 全クライアントへ出現

      // 一発リロードする (存在する場合のみ)
      var reloader = obj.GetComponent<UsdAutoReloaderBehaviour>();
      if (reloader != null)
      {
        reloader.TryReload();
      }
    }
  }
}
#else
using UnityEngine;
public class NGOSpawner : MonoBehaviour
{
  [SerializeField] private bool logOnce = true;
  void Awake() { if (logOnce) Debug.Log("[NGOSpawner] NGOSpawner disabled (HAS_UNITY_USD not defined).", this); }
}
#endif