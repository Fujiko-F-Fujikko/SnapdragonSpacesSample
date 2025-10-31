using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// サーバから catalogUrl と key を受け取って、
/// Addressables のカタログ再ロード→依存DL→再スポーン まで行うクライアント側の実装。
/// </summary>
public class AddressablesRefreshClient : MonoBehaviour
{
  [Header("Spawn")]
  [SerializeField] Transform parent;
  [SerializeField] Vector3 spawnPosition = Vector3.zero;
  [SerializeField] Vector3 spawnEuler = Vector3.zero;

  [SerializeField] bool preDownloadDependencies = true;

  GameObject spawned;
  AsyncOperationHandle<IResourceLocator> currentCatalogHandle;
  bool hasCatalogHandle;
  bool isBusy;

  public async void RefreshFrom(string remoteCatalogUrl, string initialKey)
  {
    Debug.Log($"[AddrClient] Refreshing from catalogUrl={remoteCatalogUrl}, key={initialKey}");

    if (isBusy) return;
    isBusy = true;

    // 既存を破棄
    if (spawned != null)
    {
      Addressables.ReleaseInstance(spawned);
      spawned = null;
    }
    if (hasCatalogHandle && currentCatalogHandle.IsValid())
    {
      Addressables.Release(currentCatalogHandle);
      hasCatalogHandle = false;
    }

    try
    {
      // 1) カタログ読み込み
      currentCatalogHandle = Addressables.LoadContentCatalogAsync(remoteCatalogUrl);
      await currentCatalogHandle.Task;

      if (!currentCatalogHandle.IsValid() ||
          currentCatalogHandle.Status != AsyncOperationStatus.Succeeded)
      {
        Debug.LogError($"[AddrClient] Failed to load catalog: {remoteCatalogUrl}");
        isBusy = false; return;
      }
      hasCatalogHandle = true;
      Debug.Log($"[AddrClient] Catalog loaded: {remoteCatalogUrl}");

      // 2) 依存ダウンロード（任意）
      if (preDownloadDependencies && !string.IsNullOrEmpty(initialKey))
      {
        var deps = Addressables.DownloadDependenciesAsync(initialKey, true);
        await deps.Task;
        if (deps.IsValid()) Addressables.Release(deps);
      }

      // 3) スポーン
      var rot = Quaternion.Euler(spawnEuler);
      var inst = Addressables.InstantiateAsync(initialKey, spawnPosition, rot, parent);
      await inst.Task;

      if (!inst.IsValid() ||
          inst.Status != AsyncOperationStatus.Succeeded ||
          inst.Result == null)
      {
        Debug.LogError($"[AddrClient] InstantiateAsync failed: {initialKey}");
        isBusy = false; return;
      }

      spawned = inst.Result;
      Debug.Log($"[AddrClient] Spawned: {spawned.name}");
    }
    catch (System.Exception e)
    {
      Debug.LogException(e);
    }
    finally
    {
      isBusy = false;
    }
  }

  void OnDestroy()
  {
    if (spawned != null)
    {
      Addressables.ReleaseInstance(spawned);
      spawned = null;
    }
    if (hasCatalogHandle && currentCatalogHandle.IsValid())
    {
      Addressables.Release(currentCatalogHandle);
      hasCatalogHandle = false;
    }
  }
}
