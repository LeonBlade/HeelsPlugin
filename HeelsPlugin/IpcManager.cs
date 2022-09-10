using System;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;

namespace HeelsPlugin
{
  public class IpcManager : IDisposable
  {
    public static readonly string GetOffsetIdentifier = "HeelsPlugin.GetOffset";
    public static readonly string OffsetChangedIdentifier = "HeelsPlugin.OffsetChanged";
    public static readonly string RegisterActorIdentifier = "HeelsPlugin.RegisterActor";

    private ICallGateProvider<float>? GetOffset;
    private ICallGateProvider<float, object?>? OffsetUpdate;
    private ICallGateSubscriber<string, float, object?>? RegisterActor;

    public IpcManager()
    {
      GetOffset = Plugin.PluginInterface.GetIpcProvider<float>(IpcManager.GetOffsetIdentifier);
      OffsetUpdate = Plugin.PluginInterface.GetIpcProvider<float, object?>(IpcManager.OffsetChangedIdentifier);
      RegisterActor = Plugin.PluginInterface.GetIpcSubscriber<string, float, object?>(IpcManager.RegisterActorIdentifier);

      RegisterActor.Subscribe((name, offset) =>
      {
        Plugin.Memory.PlayerOffsets[name] = offset;
      });

      GetOffset.RegisterFunc(() => Plugin.Memory?.GetPlayerOffset() ?? 0);
    }

    public void OnOffsetChange(float offset)
    {
      OffsetUpdate?.SendMessage(offset);
    }

    public void Dispose()
    {
      GetOffset?.UnregisterFunc();
    }
  }
}
