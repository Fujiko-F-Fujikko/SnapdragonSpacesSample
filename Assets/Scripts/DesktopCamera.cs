using Unity.Netcode; // Netcode for GameObjects
using UnityEngine;


public class DesktopCamera : MonoBehaviour
{
  [Header("Follow Local Player (Netcode)")]
  [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 0f); // Applied in player's local space
  [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero; // Added to player rotation

  private Transform playerTransform; // Cached local player transform

  // Start is called before the first frame update
  void Start()
  {
  }

  // Update is called once per frame
  void Update()
  {
    // Try to acquire local player transform if not cached yet
    if (playerTransform == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
    {
      var localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
      if (localPlayerObject != null)
      {
        playerTransform = localPlayerObject.transform;
      }
    }

    if (playerTransform == null)
    {
      return; // Nothing to follow yet
    }

    // Follow logic
    // Offset is applied relative to player's local orientation
    transform.position = playerTransform.TransformPoint(positionOffset);
    // Always apply rotation offset
    transform.rotation = playerTransform.rotation * Quaternion.Euler(rotationOffsetEuler);

  }
}
