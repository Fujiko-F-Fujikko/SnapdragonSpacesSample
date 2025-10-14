using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // ★追加

[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
  public float moveSpeed = 5f;

  // 各クライアントで自分のプレイヤーだけ色を変える（見分け用）
  private void Start()
  {
    var rend = GetComponentInChildren<Renderer>();
    if (rend != null)
    {
      rend.material.color = IsOwner ? new Color(0.3f, 0.8f, 1f) : Color.white;
    }
  }

  void Update()
  {
    if (!IsOwner) return;

    float h = 0, v = 0;
    if (Keyboard.current != null)
    {
      h = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
      v = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
    }
    var dir = new Vector3(h, 0, v).normalized;
    if (dir.sqrMagnitude > 0) transform.position += dir * moveSpeed * Time.deltaTime;
  }
}
