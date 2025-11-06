using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerRole))]
public class PlayerDecorator : NetworkBehaviour
{
  private TextMesh _label;

  private Renderer _body;
  private Renderer _nose;
  private PlayerRole _role;
  public Color ownerColor = new Color(0.3f, 0.8f, 1f);
  public Color remoteColor = Color.white;
  public Color hostEmphasis = new Color(1f, 0.6f, 0.2f);

  void Awake()
  {
    CacheNose();
    CatchBody();
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
    // 名前が "Nose" の直下または階層内の子 Transform を探す（まずは直下）
    var noseTf = transform.Find("Nose");
    // 次に階層内も探す
    if (noseTf == null)
    {
      foreach (var t in GetComponentsInChildren<Transform>(true))
      {
        if (t.name == "Nose") { noseTf = t; break; }
      }
    }
    if (noseTf != null) _nose = noseTf.GetComponentInChildren<Renderer>();

    if (_nose == null)
    {
      var fb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      fb.name = "Nose";
      fb.transform.SetParent(transform, false);
      fb.transform.localPosition = new Vector3(0, 0.6f, 0.5f);
      fb.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
      fb.transform.localRotation = Quaternion.Euler(90, 0, 0);
      _nose = fb.GetComponent<Renderer>();
    }
  }

  void CatchBody()
  {
    // 名前が "Body" の直下または階層内の子 Transform を探す（まずは直下）
    var bodyTf = transform.Find("Body");
    // 次に階層内も探す
    if (bodyTf == null)
    {
      foreach (var t in GetComponentsInChildren<Transform>(true))
      {
        if (t.name == "Body") { bodyTf = t; break; }
      }
    }
    if (bodyTf != null) _body = bodyTf.GetComponentInChildren<Renderer>();

    if (_body == null)
    {
      var fb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
      fb.name = "Body";
      fb.transform.SetParent(transform, false);
      fb.transform.localPosition = new Vector3(0, 1f, 0);
      fb.transform.localScale = new Vector3(1f, 1f, 1f);
      fb.transform.localRotation = Quaternion.Euler(0, 0, 0);
      _body = fb.GetComponent<Renderer>();
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
    if (_body) _body.material.color = c;
    if (_label) _label.color = c;
  }
}
