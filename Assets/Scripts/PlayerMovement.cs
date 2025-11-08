using Unity.Netcode;
using UnityEngine;
#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Management;
#endif
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class PlayerMovement : NetworkBehaviour
{
  // ===== Desktop (XZのみ／重力なし) =====
  [Header("Desktop Controls (XZ only)")]
  public float floorHeight = 0f; // 床の高さ（Y座標）
  public float moveSpeed = 3.5f;
  public float sprintMultiplier = 1.5f;
  public float mouseSensitivity = 2.0f;
  public bool cameraPitch = true;
  public float pitchMin = -80f, pitchMax = 80f;

  // --- Internals ---

  // XR用
  XROrigin _xr;
  Camera _hmdCam;               // HMDカメラ

  // Desktop用
  float _yaw, _pitch;

  // 共通
  Transform _rig;               // 動かす対象
  CharacterController _rigCC;   // _rigに付いているCharacterController(Playerプレハブに付いている想定)


  // ===== Unity callbacks =====

  void Awake()
  {
    _rig = transform; // 自分自身
    _rigCC = _rig.GetComponent<CharacterController>();

  }
  public override void OnNetworkSpawn()
  {
    Debug.Log("[PlayerMovement] OnNetworkSpawn called.");
    Debug.Log($"  XRActive: {XRActive}");


    Debug.Log($"[PlayerMovement] IsOwner: {IsOwner}, IsServer: {IsServer}, IsClient: {IsClient}");
    if (!IsOwner) { enabled = false; return; }

    if (XRActive)
    {
      // XR初期化
      var xrOrigin_obj = GameObject.Find("XR Origin (XR Rig)");
      if (xrOrigin_obj == null) { Debug.LogError("XROrigin not found on this Player."); enabled = false; return; }
      _xr = xrOrigin_obj.GetComponent<XROrigin>();
      if (_xr == null) { Debug.LogError("XROrigin component not found."); enabled = false; return; }
      Debug.Log($"[PlayerMovement] XROrigin found: {_xr.gameObject.name}");

      _hmdCam = _xr.Camera;
      if (_hmdCam == null) { Debug.LogError("XR Camera not found on XROrigin."); enabled = false; return; }
      // （任意）自分のカメラとAudioListenerをONにしている前提
      //var al = _hmdCam.GetComponent<AudioListener>(); if (al) al.enabled = true;
    }
    else
    {
      // Desktop初期化
      _yaw = _rig.eulerAngles.y; _pitch = 0f;
    }
  }

  void Update()
  {
    if (!IsOwner) return;

    if (XRActive)
      UpdateXR_ByXROrigin();
    else
      UpdateDesktop();
  }

  // ローカル回転版（親空間での前方向の水平成分）
  Quaternion YawOnlyLocal(Quaternion localQ)
  {
    Vector3 f = localQ * Vector3.forward; f.y = 0f;
    if (f.sqrMagnitude < 1e-8f) return Quaternion.identity;
    return Quaternion.LookRotation(f.normalized, Vector3.up);
  }

  void UpdateXR_ByXROrigin()
  {
    Debug.Log("[PlayerMovement] UpdateXR_ByXROrigin called.");
    Debug.Log($"  _rig: {_rig.name}, _hmdCam: {_hmdCam.name}");
    if (_rig == null || _hmdCam == null) return;

    // 1) 今フレームのHMDワールド姿勢
    var headWorldPos = _hmdCam.transform.position;
    var headWorldRot = _hmdCam.transform.rotation;
    Debug.Log($"  Head World Pos: {headWorldPos}, Head World Rot: {headWorldRot}");


    var headLocalYaw = YawOnlyLocal(_hmdCam.transform.localRotation);
    Debug.Log($"  HeadLocalYaw: {headLocalYaw.eulerAngles}");

    // 3) 現在のリグ空間でのカメラ位置（Camera Offsetの変化を含む）
    var camLocalInOrigin = _xr.CameraInOriginSpacePos;
    Debug.Log($"  CamLocalInOrigin: {camLocalInOrigin}");

    _rig.transform.position = camLocalInOrigin;
    _rig.transform.rotation = headLocalYaw;

    // これは吹っ飛ぶ
    //_rig.transform.position = headWorldPos;
    //_rig.transform.rotation = headWorldRot;
  }

  // ===== Desktop：XR Origin を WASD＋マウスで操作（XZのみ・重力なし） =====
  void UpdateDesktop()
  {
    // マウスルック（YawはOrigin、Pitchはカメラ）
    float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
    float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

    _yaw += mx;
    _rig.rotation = Quaternion.Euler(0f, _yaw, 0f);

    if (cameraPitch && _hmdCam != null)
    {
      _pitch = Mathf.Clamp(_pitch - my, pitchMin, pitchMax);
      _hmdCam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // XZのみ移動
    Vector2 inMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    if (inMove.sqrMagnitude > 1f) inMove.Normalize();

    float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
    Vector3 moveXZ = (_rig.right * inMove.x + _rig.forward * inMove.y) * speed;

    if (_rigCC) _rigCC.Move(moveXZ * Time.deltaTime);
    else _rig.position += moveXZ * Time.deltaTime;

    // 高さは固定
    float height = _rigCC.height;
    _rig.position = new Vector3(_rig.position.x, height / 2f - _rigCC.center.y + floorHeight, _rig.position.z);
  }

  // ===== XRの有効判定（Android 実機は常にXR扱い） =====
  bool XRActive
  {
    get
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true; // Androidビルドは常にXR扱い（Quest/Pico等）
#else
#if UNITY_XR_MANAGEMENT
            var g = XRGeneralSettings.Instance;
            if (g != null && g.Manager != null && g.Manager.activeLoader != null) return true;
#endif
      return XRSettings.enabled; // フォールバック
#endif
    }
  }
}
