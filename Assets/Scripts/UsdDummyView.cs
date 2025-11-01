// UsdDummyView.cs
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

  MeshRenderer _renderer;

  public override void OnNetworkSpawn()
  {
    // 1) 位置だけはすぐ反映してOK
    ApplyTransform(worldPos.Value, worldRot.Value, worldScale.Value);

    // 2) 一旦見た目を用意しておく
    _renderer = EnsureVisual(usdPath.Value.ToString());

    // 3) いま持ってる色を一旦塗る（多分まだ黒）
    ApplyColor(displayColor.Value);

    // 4) → 本物の色がサーバーから届いたら塗りなおす
    displayColor.OnValueChanged += OnColorChanged;

    // 5) → 本物のPathがサーバーから届いたら見た目を更新する
    usdPath.OnValueChanged += OnUsdPathChanged;
  }

  public override void OnNetworkDespawn()
  {
    // 念のため解除
    displayColor.OnValueChanged -= OnColorChanged;
    usdPath.OnValueChanged -= OnUsdPathChanged;
  }

  void OnColorChanged(Color prev, Color now)
  {
    Debug.Log($"[USD Dummy] Color changed from {prev} to {now}");
    ApplyColor(now);
  }

  private void OnUsdPathChanged(FixedString128Bytes prev, FixedString128Bytes now)
  {
    Debug.Log($"[USD Dummy] USD Path changed from {prev} to {now}");
    _renderer = EnsureVisual(now.ToString());

    //ApplyColor(displayColor.Value);
  }

  void ApplyTransform(Vector3 pos, Quaternion rot, Vector3 scale)
  {
    transform.position = pos;
    transform.rotation = rot;
    transform.localScale = scale;
  }

  MeshRenderer EnsureVisual(string path)
  {
    if (_renderer != null)
    {
      // すでにある場合は一度削除する
      Destroy(_renderer.gameObject);
    }

    Debug.Log($"[USD Dummy] EnsureVisual for path: {path}");
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.transform.SetParent(transform, false);
    gameObject.name = path;

    // Colliderは不要なので消す
    var col = go.GetComponent<Collider>();
    if (col != null) Destroy(col);

    return go.GetComponent<MeshRenderer>();
  }

  void ApplyColor(Color c)
  {
    if (_renderer == null) return;

    // 共有マテリアルを汚さない
    if (!(_renderer.material is null))
    {
      // URPだと _BaseColor を持ってることが多いので両方書いておく
      var mat = _renderer.material;
      mat.color = c;
      if (mat.HasProperty("_BaseColor"))
        mat.SetColor("_BaseColor", c);
    }
  }

  // サーバーからSpawn後に呼ぶ
  public void ServerInit(string path, Vector3 pos, Quaternion rot, Vector3 scale, Color col)
  {
    if (!IsServer) return;

    usdPath.Value = path;
    worldPos.Value = pos;
    worldRot.Value = rot;
    worldScale.Value = scale;
    displayColor.Value = col;  // ← これが届いた瞬間にクライアントで色が塗りなおる

    // サーバー自身も反映
    ApplyTransform(pos, rot, scale);
    _renderer = EnsureVisual(path);
    ApplyColor(col);
  }
}
