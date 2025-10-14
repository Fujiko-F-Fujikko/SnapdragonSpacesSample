using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerRole : NetworkBehaviour
{
  public NetworkVariable<FixedString32Bytes> Role =
      new NetworkVariable<FixedString32Bytes>(
          "UNKNOWN",
          NetworkVariableReadPermission.Everyone,
          NetworkVariableWritePermission.Server);

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();
    if (IsServer)
      AssignRoleOnServer();
    Role.OnValueChanged += (_, __) => SendMessage("RefreshVisual", SendMessageOptions.DontRequireReceiver);
  }

  public override void OnGainedOwnership()
  {
    if (IsServer) AssignRoleOnServer();
  }

  public override void OnLostOwnership()
  {
    if (IsServer) AssignRoleOnServer();
  }

  private void AssignRoleOnServer()
  {
    var nm = NetworkManager.Singleton;
    if (nm == null) return;

    // この Player のオーナーがサーバ自身かどうかで判定
    bool ownedByServer = OwnerClientId == NetworkManager.ServerClientId;

    if (ownedByServer)
      Role.Value = nm.IsHost ? "HOST" : "SERVER";
    else
      Role.Value = "CLIENT";
  }
}
