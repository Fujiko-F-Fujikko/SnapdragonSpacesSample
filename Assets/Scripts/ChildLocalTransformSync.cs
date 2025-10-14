using Unity.Netcode;
using UnityEngine;

public class ChildLocalTransformSync : NetworkBehaviour
{
  [SerializeField] private Transform target; // ← StoolWooden_1 を割り当て

  // サーバのみ書き込み、全員が読み取り
  private readonly NetworkVariable<Vector3> _pos =
      new(readPerm: NetworkVariableReadPermission.Everyone,
          writePerm: NetworkVariableWritePermission.Server);
  private readonly NetworkVariable<Quaternion> _rot =
      new(readPerm: NetworkVariableReadPermission.Everyone,
          writePerm: NetworkVariableWritePermission.Server);
  private readonly NetworkVariable<Vector3> _scale =
      new(readPerm: NetworkVariableReadPermission.Everyone,
          writePerm: NetworkVariableWritePermission.Server);

  // 変化検出のしきい値（お好みで）
  const float EpsPos = 0.0005f;
  const float EpsAng = 0.05f;

  void OnEnable()
  {
    _pos.OnValueChanged += (_, v) => { if (!IsServer && target) target.localPosition = v; };
    _rot.OnValueChanged += (_, v) => { if (!IsServer && target) target.localRotation = v; };
    _scale.OnValueChanged += (_, v) => { if (!IsServer && target) target.localScale = v; };
  }

  void Start()
  {
    // 初期値をクライアントへ
    if (IsServer && target)
    {
      _pos.Value = target.localPosition;
      _rot.Value = target.localRotation;
      _scale.Value = target.localScale;
    }
  }

  void Update()
  {
    // サーバだけが監視して反映を配信
    if (!IsServer || !target) return;

    // Reload(false) による “その場” 更新もここで拾える
    var p = target.localPosition;
    var r = target.localRotation;
    var s = target.localScale;

    if ((p - _pos.Value).sqrMagnitude > EpsPos * EpsPos) _pos.Value = p;

    // 角度差の簡易チェック
    var ang = Quaternion.Angle(_rot.Value, r);
    if (ang > EpsAng) _rot.Value = r;

    if ((s - _scale.Value).sqrMagnitude > EpsPos * EpsPos) _scale.Value = s;

    // もし Reload(false) で target 参照が失われたら取り直す（名称で再解決）
    if (target == null) target = transform.Find("StoolWooden_1");
  }
}
