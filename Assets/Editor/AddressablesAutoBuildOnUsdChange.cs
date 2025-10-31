#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;


/// <summary>
/// USDリロードを検知して Addressables を自動ビルドし、
/// 生成された catalog の URL を NGO で全クライアントへ通知する。
/// Editor 内だけで動く実験用ユーティリティ。
/// </summary>
[InitializeOnLoad]
public static class AddressablesAutoBuildOnUsdChange
{
  // 実験用：ホストURLのベース（ローカルホスティングに合わせる）
  // 例: http://<サーバIP>:<HostingPort>/<BuildTarget>/
  static string RemoteBaseUrl =>
      "http://192.168.85.55:64434"; // ←あなたの環境に合わせて

  // 通知に使う Addressables の key（Prefabのキー）
  static string InitialKey =>
      "Assets/Prefabs/SampleCube_prefab.prefab"; // ←あなたのキーに合わせて

  // バッチビルド用に作る「一時プロジェクト」の場所（書き込み可のパス）
  static readonly string TempProjectRoot =
      Path.Combine(Path.GetTempPath(), "AddrBuildProjectCopy");

  // デバウンス（USD保存の連打抑制）
  const int DebounceMs = 800;
  // ===========================================================

  static DateTime _lastTrigger = DateTime.MinValue;
  static bool _building;


  static AddressablesAutoBuildOnUsdChange()
  {
    // Editor起動時にフック
    UsdAutoReloaderBehaviour.OnUsdReloaded -= OnUsdReloaded;
    UsdAutoReloaderBehaviour.OnUsdReloaded += OnUsdReloaded;

    EditorApplication.quitting += () => { try { if (Directory.Exists(TempProjectRoot)) { /*残す*/ } } catch { } };

  }

  static void OnUsdReloaded()
  {
    UnityEngine.Debug.Log("[AutoBuild] Detected USD reload.");

    var now = DateTime.UtcNow;
    if ((now - _lastTrigger).TotalMilliseconds < DebounceMs) return;
    _lastTrigger = now;

    if (_building) return;
    _building = true;

    UnityEngine.Debug.Log("[AutoBuild] USD changed -> spawn batch Unity to build Addressables.");

    try
    {
      // 1) プロジェクトの軽量コピーを作成/更新（Assets, Packages, ProjectSettings など）
      string src = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
      PrepareProjectCopy(src, TempProjectRoot);

      var buildStartUtc = DateTime.UtcNow;

      // 2) バッチ Unity でコピー側プロジェクトをビルド
      RunBatchUnityBuild(TempProjectRoot, EditorUserBuildSettings.activeBuildTarget.ToString(), async (exitCode, logPath) =>
      {
        EditorApplication.delayCall += () => PollResultAndBroadcast(TempProjectRoot, logPath);
      });
    }
    catch (Exception e)
    {
      UnityEngine.Debug.LogException(e);
      _building = false;
    }
  }


  // ==========================================================

  static void MirrorDirectory(string src, string dst)
  {
    if (!Directory.Exists(src)) return;
    Directory.CreateDirectory(dst);

    // ファイルコピー（新規/更新のみ）
    foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
    {
      var rel = file.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var dest = Path.Combine(dst, rel);
      Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
      if (!File.Exists(dest) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest))
      {
        File.Copy(file, dest, true);
      }
    }

    // 余剰ファイル削除（簡易）
    foreach (var file in Directory.EnumerateFiles(dst, "*", SearchOption.AllDirectories))
    {
      var rel = file.Substring(dst.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var srcPath = Path.Combine(src, rel);
      if (!File.Exists(srcPath)) File.Delete(file);
    }
  }
  static void PrepareProjectCopy(string srcRoot, string dstRoot)
  {
    // 初回は全部作る。2回目以降は差分上書き（Library はコピーしない）
    Directory.CreateDirectory(dstRoot);

    // 必要最低限のフォルダのみミラー（Assets / Packages / ProjectSettings / UserSettings）
    MirrorDirectory(Path.Combine(srcRoot, "Assets"), Path.Combine(dstRoot, "Assets"));
    MirrorDirectory(Path.Combine(srcRoot, "Packages"), Path.Combine(dstRoot, "Packages"));
    MirrorDirectory(Path.Combine(srcRoot, "ProjectSettings"), Path.Combine(dstRoot, "ProjectSettings"));
    var us = Path.Combine(srcRoot, "UserSettings");
    if (Directory.Exists(us)) MirrorDirectory(us, Path.Combine(dstRoot, "UserSettings"));
  }

  static void WriteHelperBuilderIfMissing(string projectPath, bool overwrite = false)
  {
    string editorDir = Path.Combine(projectPath, "Assets/Editor");
    Directory.CreateDirectory(editorDir);

    string helperPath = Path.Combine(editorDir, "AddrBatchHelper.cs");
    if (!overwrite && File.Exists(helperPath))
      return;

    // 文字列は verbatim (@) を使い、ダブルクォートは "" と二重にします。
    const string HelperSource = @"
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[Serializable]
public class AddrBuildResult
{
    public string aaRoot;
    public string catalog;
    public string timestampUtc;
    public string error;
}

public static class AddrBatchHelper
{
    // batchmode: -executeMethod AddrBatchHelper.BuildAddressables
    public static void BuildAddressables()
    {
        string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ""..""));
        string resultPath  = Path.Combine(projectPath, ""AddrBuildResult.json"");

        var result = new AddrBuildResult();
        try
        {
            // Addressables の通常ビルド（Remote Catalog 有効前提）
            AddressableAssetSettings.BuildPlayerContent();

            // aa 出力ルートの推定
            string buildTargetFolder = GetTargetFolder(EditorUserBuildSettings.activeBuildTarget);
            string aaRoot = Path.Combine(projectPath, ""Library\\com.unity.addressables\\aa"", buildTargetFolder);
            result.aaRoot = aaRoot;

            // 最新 catalog を特定
            if (Directory.Exists(aaRoot))
            {
                result.catalog = Directory.EnumerateFiles(aaRoot, ""catalog*.json"", SearchOption.AllDirectories)
                                          .OrderByDescending(File.GetLastWriteTimeUtc)
                                          .FirstOrDefault() ?? string.Empty;
            }
            else
            {
                result.catalog = string.Empty;
            }

            result.timestampUtc = DateTime.UtcNow.ToString(""o"");

            var json = JsonUtility.ToJson(result, false);
            File.WriteAllText(resultPath, json);
            UnityEngine.Debug.Log(""[AddrBatchHelper] Wrote "" + resultPath + ""\n"" + json);
        }
        catch (Exception e)
        {
            result.timestampUtc = DateTime.UtcNow.ToString(""o"");
            result.error = e.ToString();
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, false));
            throw;
        }
    }

    static string GetTargetFolder(BuildTarget t)
    {
        switch (t)
        {
            case BuildTarget.Android: return ""Android"";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64: return ""Windows"";
            case BuildTarget.StandaloneOSX: return ""StandaloneOSX"";
            default: return t.ToString();
        }
    }
}
#endif
";

    File.WriteAllText(helperPath, HelperSource);
    UnityEngine.Debug.Log($"[AutoBuild] Wrote helper: {helperPath}");
  }

  static void RunBatchUnityBuild(string projectPath, string buildTarget, Action<int, string> onExit)
  {
    string editorPath = EditorApplication.applicationPath; // Unity.exe or Unity.app/.../Unity
    string logPath = Path.GetFullPath(Path.Combine(projectPath, "AddrBatch.log"));

    // コピー側プロジェクトにビルド用 Editor スクリプトを自動配置（1回でOK）
    WriteHelperBuilderIfMissing(projectPath);

    string args =
        $"-batchmode -quit -nographics -projectPath \"{projectPath}\" " +
        $"-executeMethod AddrBatchHelper.BuildAddressables " +
        $"-buildTarget {buildTarget} -logFile \"{logPath}\"";

    var psi = new ProcessStartInfo
    {
      FileName = editorPath,
      Arguments = args,
      UseShellExecute = false,
      CreateNoWindow = true,
    };

    var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
    proc.Exited += (_, __) =>
    {
      onExit?.Invoke(proc.ExitCode, logPath);
      proc.Dispose();
    };

    if (!proc.Start())
    {
      UnityEngine.Debug.LogError("[AutoBuild] Failed to start batch Unity.");
      onExit?.Invoke(-1, logPath);
    }
  }

  // ==========================================================



  // ==========================================================

  static void CopyDirectoryWhole(string srcRoot, string dstRoot)
  {
    if (string.IsNullOrEmpty(srcRoot) || string.IsNullOrEmpty(dstRoot))
      throw new ArgumentException("srcRoot/dstRoot is null or empty.");
    if (!Directory.Exists(srcRoot))
      throw new DirectoryNotFoundException($"Source not found: {srcRoot}");

    // 既存を完全削除してからコピー（“まるごとコピー”）
    if (Directory.Exists(dstRoot))
      Directory.Delete(dstRoot, recursive: true);

    Directory.CreateDirectory(dstRoot);

    // ディレクトリを先に作る
    foreach (var dir in Directory.EnumerateDirectories(srcRoot, "*", SearchOption.AllDirectories))
    {
      var rel = dir.Substring(srcRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      Directory.CreateDirectory(Path.Combine(dstRoot, rel));
    }

    // ファイルをコピー
    foreach (var file in Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories))
    {
      var rel = file.Substring(srcRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      var dst = Path.Combine(dstRoot, rel);
      Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
      File.Copy(file, dst, overwrite: true);
    }
  }

  static void BroadcastToClients(string catalogUrl, string key)
  {
    var nm = NetworkManager.Singleton;
    if (nm == null || !nm.IsServer)
    {
      UnityEngine.Debug.LogWarning("[AutoBuild] NetworkManager not found or not server. Skip broadcast.");
      return;
    }

    var notifier = UnityEngine.Object.FindObjectOfType<AddressablesRefreshNotifier>();
    if (notifier == null)
    {
      UnityEngine.Debug.LogWarning("[AutoBuild] AddressablesRefreshNotifier not found in scene.");
      return;
    }

    notifier.BroadcastRefresh(catalogUrl, key);
  }

  static void PollResultAndBroadcast(string tempProjectRoot, string logPath)
  {
    string resultPath = Path.Combine(tempProjectRoot, "AddrBuildResult.json");
    float timeout = 30f;  // 最大30秒待つ
    float elapsed = 0f;

    void Tick()
    {
      if (File.Exists(resultPath))
      {
        try
        {
          string buildTargetFolder = GetActiveBuildTargetFolder();

          // ★ src = Temp 側 ServerData/<Platform>
          string srcRoot = Path.Combine(tempProjectRoot, "ServerData", buildTargetFolder);
          if (!Directory.Exists(srcRoot))
          {
            UnityEngine.Debug.LogError($"[AutoBuild] Temp ServerData/{buildTargetFolder} not found.");
            return;
          }

          // ★ dst = 元プロジェクト側 ServerData/<Platform>
          //    プロジェクトルートは現在の Editor 側なので、相対でも OK ですが念のため絶対に:
          string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
          string dstRoot = Path.Combine(projectRoot, "ServerData", buildTargetFolder);

          // srcRoot 内から catalog_*.json を探す
          string latestCatalog = Directory.EnumerateFiles(srcRoot, "catalog_*.json", SearchOption.AllDirectories)
              .OrderByDescending(File.GetLastWriteTimeUtc)
              .FirstOrDefault();

          if (latestCatalog == null)
          {
            UnityEngine.Debug.LogError($"[AutoBuild] No catalog found under srcRoot: {srcRoot}");
            return;
          }

          // catalogファイル名を取得
          string catalogFileName = Path.GetFileName(latestCatalog);
          UnityEngine.Debug.Log($"[AutoBuild] Latest catalog: {latestCatalog}");

          // ServerData/<Platform> に丸ごとコピー
          CopyDirectoryWhole(srcRoot, dstRoot);

          // 更新通知URLを生成（catalogファイル名＋タイムスタンプでキャッシュバスター）
          long ver = File.GetLastWriteTimeUtc(latestCatalog).Ticks;
          string url = $"{RemoteBaseUrl}/{catalogFileName}?v={ver}";

          BroadcastToClients(url, InitialKey);
          UnityEngine.Debug.Log($"[AutoBuild] Broadcast catalog: {url}");
        }
        catch (Exception e)
        {
          UnityEngine.Debug.LogException(e);
        }
        finally
        {
          EditorApplication.update -= UpdateLoop;
          _building = false;
        }
        return;
      }

      elapsed += Time.unscaledDeltaTime;
      if (elapsed > timeout)
      {
        UnityEngine.Debug.LogError($"[AutoBuild] Timeout waiting for build result. Log: {logPath}");
      }
    }

    void UpdateLoop() => Tick();

    // ポーリング開始
    EditorApplication.update += UpdateLoop;
  }

  // ==========================================================

  static string GetActiveBuildTargetFolder()
  {

    var t = EditorUserBuildSettings.activeBuildTarget;
    switch (t)
    {
      case BuildTarget.Android: return "Android";
      case BuildTarget.StandaloneWindows:
      case BuildTarget.StandaloneWindows64: return "StandaloneWindows64";
      case BuildTarget.StandaloneOSX: return "StandaloneOSX";
      default: return t.ToString();
    }
  }

}
#endif