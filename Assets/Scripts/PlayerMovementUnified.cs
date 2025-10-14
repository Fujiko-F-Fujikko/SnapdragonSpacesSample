using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // 新Input System対応
using UnityEngine.XR;

/// <summary>
/// VR接続時はHMD＋スティックで移動、
/// 非VR時はWASD＋マウス視点で操作する統合スクリプト。
/// 自分が所有しているPlayerのみ操作できる。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementUnified : NetworkBehaviour
{
  [Header("移動設定")]
  public float moveSpeed = 2.5f;
  public float turnSpeed = 90f;
  public float mouseSensitivity = 3f;

  [Header("参照設定")]
  private Camera playerCamera;  // FPSモードのカメラ or VR HMDカメラ

  private CharacterController controller;
  private bool isVRActive = false;
  private float verticalLookRotation = 0f;

  private void Awake()
  {
    controller = GetComponent<CharacterController>();
    isVRActive = XRSettings.isDeviceActive;
    playerCamera = Camera.main;
  }

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();

    // 所有しているPlayerのみカメラON
    if (playerCamera != null)
      playerCamera.enabled = IsOwner;
  }

  void Update()
  {
    if (!IsOwner) return;

    if (isVRActive)
      HandleVRMovement();
    else
      HandleDesktopMovement();
  }

  // ----------- VRモード -----------
  void HandleVRMovement()
  {
    UnityEngine.XR.InputDevice leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    UnityEngine.XR.InputDevice rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

    Vector2 moveInput = Vector2.zero;
    Vector2 turnInput = Vector2.zero;

    leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out moveInput);
    rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out turnInput);

    // HMDの向きに合わせて移動
    if (playerCamera == null)
    {
      Debug.LogWarning("PlayerMovementUnified: playerCamera is null in VR mode.");
      return;
    }
    Transform head = playerCamera.transform;
    Vector3 forward = new Vector3(head.forward.x, 0, head.forward.z).normalized;
    Vector3 right = new Vector3(head.right.x, 0, head.right.z).normalized;

    Vector3 move = (forward * moveInput.y + right * moveInput.x) * moveSpeed;
    controller.Move(move * Time.deltaTime);

    // 右スティックで回転
    if (Mathf.Abs(turnInput.x) > 0.2f)
      transform.Rotate(Vector3.up, turnInput.x * turnSpeed * Time.deltaTime);
  }

  // ----------- PCモード -----------
  void HandleDesktopMovement()
  {
    var keyboard = Keyboard.current;
    if (keyboard == null) return;

    Vector3 move = Vector3.zero;
    if (keyboard.wKey.isPressed) move += transform.forward;
    if (keyboard.sKey.isPressed) move -= transform.forward;
    if (keyboard.aKey.isPressed) move -= transform.right;
    if (keyboard.dKey.isPressed) move += transform.right;

    controller.Move(move.normalized * moveSpeed * Time.deltaTime);

    // マウス視点回転
    var mouse = Mouse.current;
    if (mouse != null)
    {
      float mouseX = mouse.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
      float mouseY = mouse.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

      transform.Rotate(Vector3.up * mouseX);
      verticalLookRotation -= mouseY;
      verticalLookRotation = Mathf.Clamp(verticalLookRotation, -80f, 80f);

      if (playerCamera != null)
        playerCamera.transform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
    }
  }
}
