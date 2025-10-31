using Unity.Netcode;
using UnityEngine;

/// <summary>
/// サーバ側に置いて、最新 catalogUrl / key を全クライアントへ配る。
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class AddressablesRefreshNotifier : NetworkBehaviour
{
  public void BroadcastRefresh(string catalogUrl, string key)
  {
    Debug.Log($"[Notifier] Broadcasting catalog refresh: catalogUrl={catalogUrl}, key={key}");

    if (!IsServer)
    {
      Debug.LogWarning("[Notifier] Only server can broadcast.");
      return;
    }
    ClientsRefreshClientRpc(catalogUrl, key);
  }

  [ClientRpc]
  void ClientsRefreshClientRpc(string catalogUrl, string key)
  {
    Debug.Log($"[Notifier] Received catalog refresh: catalogUrl={catalogUrl}, key={key}");

    // クライアント側で受信：リフレッシュへ委譲
    var client = FindObjectOfType<AddressablesRefreshClient>();
    if (client != null)
    {
      client.RefreshFrom(catalogUrl, key);
    }
    else
    {
      Debug.LogWarning("[Notifier] AddressablesRefreshClient not found on client scene.");
    }
  }
}
