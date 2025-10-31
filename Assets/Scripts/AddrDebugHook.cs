// AddrDebugHook.cs
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;

public static class AddrDebugHook
{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Hook()
  {
    ResourceManager.ExceptionHandler += (op, ex) =>
    {
      var id = op.IsValid() ? op.DebugName : "(no operation)";
      Debug.LogError($"[Addr] Failed at: {id}\n{ex}");
    };
  }
}
