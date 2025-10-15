using System.Collections;
using Qualcomm.Snapdragon.Spaces;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class EnablePassthroughRobust : MonoBehaviour
{
  [SerializeField] SpacesLifecycleEvents lifecycle; // ヒエラルキーから割当
  [SerializeField] float retryInterval = 0.1f;      // 100ms
  [SerializeField] float retryTimeout = 5.0f;      // 最大5秒

  BaseRuntimeFeature _feature;
  Coroutine _routine;

  void Awake()
  {
    if (!lifecycle) lifecycle = FindObjectOfType<SpacesLifecycleEvents>();
    lifecycle.OnOpenXRStarted.AddListener(OnOpenXRStarted);
  }
  void OnDestroy()
  {
    if (lifecycle) lifecycle.OnOpenXRStarted.RemoveListener(OnOpenXRStarted);
  }

  void OnOpenXRStarted()
  {
    Debug.Log("[PT] OpenXR started. Will enable passthrough when ready...");
    _feature = OpenXRSettings.Instance?.GetFeature<BaseRuntimeFeature>();
    if (_routine != null) StopCoroutine(_routine);
    _routine = StartCoroutine(EnableWhenReady());
  }

  IEnumerator EnableWhenReady()
  {
    float t = 0f;
    while (t < retryTimeout)
    {
      Debug.Log("[PT] Enable passthrough trial [" + t + "]sec...");

      // 1) Feature が使える状態か（OpenXR/Runtimeの準備OK？）
      bool usable = (_feature != null) && FeatureUseCheckUtility.IsFeatureUseable(_feature);

      // 2) デバイス/ランタイム側が「パススルー対応」と答えたか
      bool supported = usable && _feature.IsPassthroughSupported();

      // 3) すでにONなら終了
      if (supported && _feature.GetPassthroughEnabled())
      {
        Debug.Log("[PT] Already enabled.");
        yield break;
      }

      if (supported)
      {
        _feature.SetPassthroughEnabled(true);
        // 直後に反映されたか確認してから抜ける
        yield return null;
        if (_feature.GetPassthroughEnabled())
        {
          Debug.Log("[PT] Enabled once runtime became ready.");
          yield break;
        }
      }

      yield return new WaitForSeconds(retryInterval);
      t += retryInterval;
    }
    Debug.LogWarning("[PT] Timed out waiting for passthrough readiness.");
  }
}
