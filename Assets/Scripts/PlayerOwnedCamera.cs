using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;

public class PlayerOwnedCamera : NetworkBehaviour
{
  [SerializeField] private GameObject XROrigin;
  [SerializeField] private GameObject SpacesHostView;
  public override void OnNetworkSpawn() { Apply(); }
  public override void OnGainedOwnership() { Apply(); }
  public override void OnLostOwnership() { Apply(); }

  void Start() { Apply(); }

  void Apply()
  {
    // 「自分がクライアント かつ このプレイヤーのオーナー」の時だけカメラを有効
    bool enable = IsClient && IsOwner;

    if (XROrigin) XROrigin.SetActive(enable);
    if (SpacesHostView) SpacesHostView.SetActive(enable);

    // Cinemachine を使っているなら Brain/VirtualCamera も同様に enable を切替
    // var brain = Camera.main?.GetComponent<CinemachineBrain>(); など
  }
}
