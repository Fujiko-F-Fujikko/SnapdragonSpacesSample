#if HAS_UNITY_USD
using Unity.Netcode;
using UnityEngine;

public class NGOSpawner : NetworkBehaviour
{
  [SerializeField] private NetworkObject networkObject; // networkObjectを割り当て
  [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
  [SerializeField] private Vector3 spawnRotation = new Vector3(0f, 0f, 0f);
  [SerializeField] private Vector3 spawnScale = new Vector3(1.0f, 1.0f, 1.0f);

  public override void OnNetworkSpawn()
  {
    if (!IsServer) return;
    var obj = Instantiate(networkObject, spawnPosition, Quaternion.Euler(spawnRotation));
    obj.transform.localScale = spawnScale;
    obj.Spawn(true); // 全クライアントへ出現

    // 一発リロードする
    var reloader = obj.GetComponent<UsdAutoReloaderBehaviour>();
    if (reloader != null)
    {
      reloader.TryReload();
    }
  }
}
# else
using UnityEngine;
public class NGOSpawner : MonoBehaviour
{
  [SerializeField] private bool logOnce = true;
  void Awake() { if (logOnce) Debug.Log("[USD-Net] NGOSpawner disabled (HAS_UNITY_USD not defined).", this); }
}
#endif