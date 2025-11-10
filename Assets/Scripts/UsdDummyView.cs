using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class UsdDummyView : NetworkBehaviour
{
  public NetworkVariable<FixedString128Bytes> usdPath = new();
  public NetworkVariable<Vector3> worldPos = new();
  public NetworkVariable<Quaternion> worldRot = new();
  public NetworkVariable<Vector3> worldScale = new();
  public NetworkVariable<Color> displayColor =
      new(writePerm: NetworkVariableWritePermission.Server);

  // ★ 追加: どの見た目を出すか（サーバーが決める）
  // 0 = Cube, 1 = Sphere, 2 = Cylinder, 10〜 = カスタムMesh
  public NetworkVariable<byte> visualKind =
      new(writePerm: NetworkVariableWritePermission.Server);

  // ★ 追加: クライアントが持ってる固定メッシュたち
  // インスペクタで入れておく
  [SerializeField] private Mesh[] meshTable;


  MeshRenderer _renderer;

  public override void OnNetworkSpawn()
  {
    Debug.Log($"[UsdDummyView] UsdDummyView spawned: {usdPath.Value}", this);

    // Transform
    ApplyTransform(worldPos.Value, worldRot.Value, worldScale.Value);

    // 見た目を今ある値で作る
    _renderer = EnsureVisual(visualKind.Value, usdPath.Value.ToString());

    // 色を一旦適用
    ApplyColor(displayColor.Value);

    // コールバック
    displayColor.OnValueChanged += OnColorChanged;
    usdPath.OnValueChanged += OnUsdPathChanged;
    visualKind.OnValueChanged += OnVisualKindChanged;
  }

  public override void OnNetworkDespawn()
  {
    displayColor.OnValueChanged -= OnColorChanged;
    usdPath.OnValueChanged -= OnUsdPathChanged;
    visualKind.OnValueChanged -= OnVisualKindChanged;
  }

  void OnColorChanged(Color prev, Color now)
  {
    ApplyColor(now);
  }

  void OnUsdPathChanged(FixedString128Bytes prev, FixedString128Bytes now)
  {
    // 名前だけ変えるならこれでOK
    gameObject.name = now.ToString();
  }

  void OnVisualKindChanged(byte prev, byte now)
  {
    Debug.Log($"[UsdDummyView] VisualKind changed: {prev} -> {now}", this);

    // 見た目を作り直す
    _renderer = EnsureVisual(now, usdPath.Value.ToString());
    ApplyColor(displayColor.Value);
  }

  void ApplyTransform(Vector3 pos, Quaternion rot, Vector3 scale)
  {
    transform.position = pos;
    transform.rotation = rot;
    transform.localScale = scale;
  }

  // ★ここで「種類に応じて」見た目を作る
  MeshRenderer EnsureVisual(byte kind, string path)
  {
    // すでにあったら消す
    if (_renderer != null)
    {
      Destroy(_renderer.gameObject);
      _renderer = null;
    }

    GameObject go = null;

    if (kind >= 10)
    {
      go = CreateFromMesh(kind);
    }
    else
    {
      switch (kind)
      {
        case 0: // Cube
          go = GameObject.CreatePrimitive(PrimitiveType.Cube);
          break;
        case 1: // Sphere
          go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
          break;
        case 2: // Cylinder
          go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
          break;
        case 3: // Camera
          go = new GameObject("Camera");
          go.AddComponent<Camera>();
          go.GetComponent<Camera>().tag = "MainCamera";
          go.AddComponent<MeshRenderer>(); // ダミーのMeshRendererを追加
          // Clientでは無効にする. Hostでは有効にする
          // (Clientで作成するとCameraが取られてしまうので)
          Debug.Log($"[UsdDummyView] Creating Camera. IsHost: {IsHost}, IsClient: {IsClient}, IsServer: {IsServer}", this);
          if (IsClient)
          {
            go.SetActive(false);
          }
          break;
        default:
          // 不明ならCubeにしちゃう
          go = GameObject.CreatePrimitive(PrimitiveType.Cube);
          break;
      }
    }

    go.transform.SetParent(transform, false);
    go.name = "Visual_" + kind.ToString();
    gameObject.name = path;

    // Colliderはいらなければ消す
    var col = go.GetComponent<Collider>();
    if (col != null) Destroy(col);

    return go.GetComponent<MeshRenderer>();
  }

  GameObject CreateFromMesh(int kind)
  {
    var go = new GameObject("Visual_" + kind.ToString());
    var mf = go.AddComponent<MeshFilter>();
    var mr = go.AddComponent<MeshRenderer>();
    mf.sharedMesh = meshTable[kind - 10];
    return go;
  }

  void ApplyColor(Color c)
  {
    if (_renderer == null) return;

    var mat = _renderer.material;
    mat.color = c;
    if (mat.HasProperty("_BaseColor"))
      mat.SetColor("_BaseColor", c);
  }

  // サーバーからSpawn後に呼ぶ
  public void ServerInit(
      string path,
      Vector3 pos,
      Quaternion rot,
      Vector3 scale,
      Color col,
      byte kind)
  {
    if (!IsServer) return;

    usdPath.Value = path;
    worldPos.Value = pos;
    worldRot.Value = rot;
    worldScale.Value = scale;
    displayColor.Value = col;
    visualKind.Value = kind;   // ★ここ

    // サーバーでも反映
    ApplyTransform(pos, rot, scale);
    _renderer = EnsureVisual(kind, path);
    ApplyColor(col);
  }
}
