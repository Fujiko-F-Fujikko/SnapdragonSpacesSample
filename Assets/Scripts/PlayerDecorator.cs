using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerDecorator : NetworkBehaviour
{
  private TextMesh _label;
  private Renderer _nose;
  private PlayerRole _role;
  public Color ownerColor = new Color(0.3f, 0.8f, 1f);
  public Color remoteColor = Color.white;
  public Color hostEmphasis = new Color(1f, 0.6f, 0.2f);

  void Awake()
  {
    CacheNose();
    EnsureLabel();
  }

  public override void OnNetworkSpawn()
  {
    base.OnNetworkSpawn();
    _role = GetComponent<PlayerRole>();

    if (_role != null)
      _role.Role.OnValueChanged += OnRoleChanged;

    // 初期状態でも一度描画
    RefreshVisual();
  }

  public override void OnNetworkDespawn()
  {
    base.OnNetworkDespawn();
    if (_role != null)
      _role.Role.OnValueChanged -= OnRoleChanged;
  }

  private void OnRoleChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
  {
    RefreshVisual();
  }

  void Update()
  {
    // ラベルをカメラ方向へ
    if (_label && Camera.main)
      _label.transform.forward = Camera.main.transform.forward;
  }

  void CacheNose()
  {
    var noseTf = transform.Find("Nose");
    if (noseTf != null) _nose = noseTf.GetComponentInChildren<Renderer>();

    if (_nose == null)
    {
      var fb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      fb.name = "Nose";
      fb.transform.SetParent(transform, false);
      fb.transform.localPosition = new Vector3(0, 0.6f, 0.5f);
      fb.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
      _nose = fb.GetComponent<Renderer>();
    }
  }

  void EnsureLabel()
  {
    var t = transform.Find("RoleLabel");
    if (t != null) _label = t.GetComponent<TextMesh>();
    if (_label == null)
    {
      var go = new GameObject("RoleLabel");
      go.transform.SetParent(transform, false);
      go.transform.localPosition = new Vector3(0, 1.5f, 0);
      _label = go.AddComponent<TextMesh>();
      _label.anchor = TextAnchor.LowerCenter;
      _label.alignment = TextAlignment.Center;
      _label.characterSize = 0.12f;
      _label.fontSize = 64;
      _label.richText = true;
    }
  }

  public void RefreshVisual()
  {
    string role = _role ? _role.Role.Value.ToString() : "UNKNOWN";
    bool isMine = IsOwner;
    ulong oid = OwnerClientId;

    string ownerStr = isMine ? "Owner" : "Remote";
    if (_label)
      _label.text = $"{role}  [{ownerStr}]\nOwnerId: {oid}";

    Color c = isMine ? ownerColor : remoteColor;
    if (role == "HOST" && isMine) c = hostEmphasis;
    if (_nose) _nose.material.color = c;
    if (_label) _label.color = c;
  }
}
