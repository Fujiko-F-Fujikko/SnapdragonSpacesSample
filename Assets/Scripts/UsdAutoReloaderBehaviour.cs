// --- USDパッケージがあるビルド（Editor/Serverなど）向け本体 ---
#if HAS_UNITY_USD
using System;
using System.IO;
using Unity.Formats.USD;
using Unity.Netcode;
using UnityEngine;


[DisallowMultipleComponent]
public class UsdAutoReloaderBehaviour : MonoBehaviour
{
  [Header("Refs")]
  [Tooltip("同じGameObjectに付いている UsdAsset。未指定なら自動取得します")]
  [SerializeField] private UsdAsset usdAsset;

  [Header("Options")]
  [Tooltip("NGO使用時、Server/Host のときだけ動かす")]
  [SerializeField] private bool onlyIfServer = true;

  [Tooltip("更新チェック間隔（秒）")]
  [SerializeField] private float pollInterval = 0.5f;

  [Tooltip("連続保存をまとめる待ち時間（秒）")]
  [SerializeField] private float debounceSeconds = 1.0f;

  [SerializeField] private bool verboseLog = false;

  string _path;
  DateTime _lastWriteUtc;
  float _nextPollAt, _scheduledAt = -1f;

  void Awake()
  {
    if (!usdAsset) usdAsset = GetComponent<UsdAsset>();
    if (!usdAsset)
    {
      Debug.LogWarning("[USD] UsdAsset が見つかりません。", this);
      enabled = false; return;
    }

    if (onlyIfServer && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer))
    {   // クライアント側では無効化
      enabled = false; return;
    }

    _path = usdAsset.usdFullPath;
    if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
    {
      Debug.LogWarning($"[USD] 監視対象ファイルが無効です: {_path}", this);
      enabled = false; return;
    }
    _path = Path.GetFullPath(_path);
    _lastWriteUtc = File.GetLastWriteTimeUtc(_path);
    _nextPollAt = Time.unscaledTime + pollInterval;
    if (verboseLog) Debug.Log($"[USD] Watching: {_path}", this);
  }

  void Update()
  {
    var now = Time.unscaledTime;

    if (now >= _nextPollAt)
    {
      _nextPollAt = now + pollInterval;
      try
      {
        var wt = File.GetLastWriteTimeUtc(_path);
        if (wt > _lastWriteUtc)
        {
          _lastWriteUtc = wt;
          _scheduledAt = now + debounceSeconds;  // デバウンス
          if (verboseLog) Debug.Log("[USD] Change detected, scheduled reload.", this);
        }
      }
      catch (Exception e)
      {
        if (verboseLog) Debug.LogWarning($"[USD] Watch error: {e.Message}", this);
      }
    }

    if (_scheduledAt > 0 && now >= _scheduledAt)
    {
      _scheduledAt = -1;
      TryReload();
    }
  }

  void TryReload()
  {
    if (!usdAsset) return;
    try
    {
      usdAsset.Reload(false);   // ★ “その場更新” (NetworkObjectを壊さない)
      if (verboseLog) Debug.Log("[USD] Reload(false) executed.", this);
    }
    catch (Exception ex)
    {
      Debug.LogException(ex, this);
    }
  }
}
#else
// --- USDパッケージが無いビルド（クライアント等）でもコンパイルが通るダミー ---
using UnityEngine;
public class UsdAutoReloaderBehaviour : MonoBehaviour
{
  [SerializeField] private bool logOnce = true;
  void Awake() { if (logOnce) Debug.Log("[USD] UsdAutoReloaderBehaviour disabled (HAS_UNITY_USD not defined).", this); }
}
#endif
