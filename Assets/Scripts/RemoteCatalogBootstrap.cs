using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RemoteCatalogBootstrap : MonoBehaviour
{
  [Header("Addressables")]
  [SerializeField]
  string remoteCatalogUrl =
      "http://192.168.85.55:64434/catalog_0.1.json";
  [SerializeField] string initialKey = "Assets/Prefabs/SampleCube_prefab.prefab";

  [SerializeField] bool preDownloadDependencies = true;

  [Header("Spawn")]
  [SerializeField] Transform parent;
  [SerializeField] Vector3 spawnPosition = Vector3.zero;
  [SerializeField] Vector3 spawnEuler = Vector3.zero;

  GameObject spawned;

  async void Awake()
  {
    // 1) カタログ読み込み
    AsyncOperationHandle<IResourceLocator> catHandle = default;
    try
    {
      catHandle = Addressables.LoadContentCatalogAsync(remoteCatalogUrl);
      await catHandle.Task;

      if (!catHandle.IsValid() ||
          catHandle.Status != AsyncOperationStatus.Succeeded)
      {
        Debug.LogError($"[Addr] Failed to load catalog: {remoteCatalogUrl}");
        return;
      }
      Debug.Log($"[Addr] Catalog loaded: {remoteCatalogUrl}");
    }
    catch (System.Exception e)
    {
      Debug.LogException(e);
      return;
    }
    // ※ catHandle は保持していてOK。ここでは解放しない（ロケータが必要）

    // 2) 依存を先にダウンロード（任意）
    AsyncOperationHandle depsHandle = default;
    if (preDownloadDependencies && !string.IsNullOrEmpty(initialKey))
    {
      try
      {
        depsHandle = Addressables.DownloadDependenciesAsync(initialKey, true);
        await depsHandle.Task;

        if (!depsHandle.IsValid() ||
            depsHandle.Status != AsyncOperationStatus.Succeeded)
        {
          Debug.LogWarning("[Addr] DownloadDependencies failed (continuing).");
        }
        else
        {
          Debug.Log("[Addr] Dependencies downloaded.");
        }
      }
      finally
      {
        if (depsHandle.IsValid())
          Addressables.Release(depsHandle);   // 解放後は触らない！
      }
    }

    // 3) 直接インスタンス化（安全）
    AsyncOperationHandle<GameObject> instHandle = default;
    try
    {
      var rot = Quaternion.Euler(spawnEuler);
      instHandle = Addressables.InstantiateAsync(initialKey, spawnPosition, rot, parent);
      await instHandle.Task;

      if (!instHandle.IsValid() ||
          instHandle.Status != AsyncOperationStatus.Succeeded ||
          instHandle.Result == null)
      {
        Debug.LogError($"[Addr] InstantiateAsync failed for key: {initialKey}");
        return;
      }

      if (spawned != null)
        Addressables.ReleaseInstance(spawned);

      spawned = instHandle.Result;
      Debug.Log($"[Addr] Spawned: {spawned.name} (key: {initialKey})");
      LogShaders(spawned);
    }
    catch (System.Exception e)
    {
      Debug.LogException(e);
    }
    finally
    {
      // InstantiateAsync のハンドルは Release しない（インスタンスの寿命と紐づいているため）
      // 破棄時に ReleaseInstance(spawned) を呼ぶ
    }
  }

  void OnDestroy()
  {
    if (spawned != null)
    {
      Addressables.ReleaseInstance(spawned);
      spawned = null;
    }
  }

  void LogShaders(GameObject go)
  {
    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
      foreach (var m in r.sharedMaterials)
        Debug.Log($"[MAT] {m?.name}  Shader={m?.shader?.name}");
  }

}
