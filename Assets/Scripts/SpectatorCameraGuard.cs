using Unity.Netcode;
using UnityEngine;

public class SpectatorCameraGuard : MonoBehaviour
{
  public Camera spectatorCamera;
  public AudioListener spectatorAudio;

  void Start()
  {
    if (spectatorCamera == null)
      spectatorCamera = GetComponentInChildren<Camera>();
    if (spectatorAudio == null)
      spectatorAudio = GetComponentInChildren<AudioListener>();

    UpdateActive();

    var nm = NetworkManager.Singleton;
    if (nm != null)
    {
      nm.OnClientConnectedCallback += OnClientChanged;
      nm.OnClientDisconnectCallback += OnClientChanged;
    }
  }

  void OnDestroy()
  {
    var nm = NetworkManager.Singleton;
    if (nm != null)
    {
      nm.OnClientConnectedCallback -= OnClientChanged;
      nm.OnClientDisconnectCallback -= OnClientChanged;
    }
  }

  void OnClientChanged(ulong _) => UpdateActive();

  void UpdateActive()
  {
    bool hasLocalPlayer = NetworkManager.Singleton != null &&
                          NetworkManager.Singleton.LocalClient != null &&
                          NetworkManager.Singleton.LocalClient.PlayerObject != null;

    bool active = !hasLocalPlayer; // ローカルに所有Playerが無ければ神様視点ON

    if (spectatorCamera) spectatorCamera.enabled = active;
    if (spectatorAudio) spectatorAudio.enabled = active;
    gameObject.SetActive(active || gameObject.activeSelf); // 任意：オブジェクト自体は残す
  }
}
