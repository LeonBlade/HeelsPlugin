using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace HeelsPlugin
{
  public class IpcManager : IDisposable
  {
    private static readonly string API_VERSION = "1.0.1";

    public static readonly string ApiVersionIdentifier = "HeelsPlugin.ApiVersion";
    public static readonly string GetOffsetIdentifier = "HeelsPlugin.GetOffset";
    public static readonly string OffsetChangedIdentifier = "HeelsPlugin.OffsetChanged";
    public static readonly string RegisterPlayerIdentifier = "HeelsPlugin.RegisterPlayer";
    public static readonly string UnregisterPlayerIdentifier = "HeelsPlugin.UnregisterPlayer";

    private ICallGateProvider<string>? ApiVersion;
    private ICallGateProvider<float>? GetOffset;
    private ICallGateProvider<float, object?>? OffsetUpdate;
    private ICallGateProvider<GameObject, float, object?>? RegisterPlayer;
    private ICallGateProvider<GameObject, object?>? UnregisterPlayer;

    public IpcManager(DalamudPluginInterface pluginInterface, PluginMemory memory)
    {
      ApiVersion = pluginInterface.GetIpcProvider<string>(ApiVersionIdentifier);
      GetOffset = pluginInterface.GetIpcProvider<float>(IpcManager.GetOffsetIdentifier);
      OffsetUpdate = pluginInterface.GetIpcProvider<float, object?>(IpcManager.OffsetChangedIdentifier);
      RegisterPlayer = pluginInterface.GetIpcProvider<GameObject, float, object?>(IpcManager.RegisterPlayerIdentifier);
      UnregisterPlayer = pluginInterface.GetIpcProvider<GameObject, object?>(IpcManager.UnregisterPlayerIdentifier);

      RegisterPlayer.RegisterAction((gameObject, offset) =>
      {
        memory.PlayerOffsets[gameObject] = offset;
      });

      UnregisterPlayer.RegisterAction((gameObject) =>
      {
        memory.PlayerOffsets.Remove(gameObject);
      });

      ApiVersion.RegisterFunc(() => API_VERSION);
      GetOffset.RegisterFunc(memory.GetPlayerOffset);
    }

    public void OnOffsetChange(float offset)
    {
      OffsetUpdate?.SendMessage(offset);
    }

    public void Dispose()
    {
      ApiVersion?.UnregisterFunc();
      GetOffset?.UnregisterFunc();
      RegisterPlayer?.UnregisterAction();
      UnregisterPlayer?.UnregisterAction();
    }
  }
}
