using Unity.Netcode;
using UnityEngine;

public class KitchenSetSpawner : NetworkBehaviour
{
  [SerializeField] private NetworkObject kitchenPrefab; // Kitchen_set_net を割り当て
  [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
  [SerializeField] private Vector3 spawnRotation = new Vector3(90f, 0f, 0f);
  [SerializeField] private Vector3 spawnScale = new Vector3(1.0f, 1.0f, 1.0f);

  public override void OnNetworkSpawn()
  {
    if (!IsServer) return;
    var obj = Instantiate(kitchenPrefab, spawnPosition, Quaternion.Euler(spawnRotation));
    obj.transform.localScale = spawnScale;
    obj.Spawn(true); // 全クライアントへ出現
  }
}
