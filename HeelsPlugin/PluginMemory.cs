using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeelsPlugin
{
  public class PluginMemory
  {
    private IntPtr actor = IntPtr.Zero;

    public IntPtr playerMovementFunc;
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void PlayerMovementDelegate(IntPtr player);
    public readonly Hook<PlayerMovementDelegate> playerMovementHook;

    public IntPtr gposeActorFunc;
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr GposeActorDelegate(IntPtr rcx, int rdx, int r8, IntPtr r9, long unknown);
    public readonly Hook<GposeActorDelegate> gposeActorHook;

    private Dictionary<short, ConfigModel> ModelCache = new();

    public PluginMemory()
    {
      playerMovementFunc = Plugin.SigScanner.ScanText("48 89 5C 24 08 55 48 8B EC 48 83 EC 70 83 79 7C 00");
      playerMovementHook = new Hook<PlayerMovementDelegate>(
        playerMovementFunc,
        new PlayerMovementDelegate(PlayerMovementHook)
      );

      gposeActorFunc = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 85 DB 74 33");
      gposeActorHook = new Hook<GposeActorDelegate>(
        gposeActorFunc,
        new GposeActorDelegate(GposeActorHook)
      );

      playerMovementHook.Enable();
      gposeActorHook.Enable();
    }

    /// <summary>
    /// Dispose for the memory functions.
    /// </summary>
    public void Dispose()
    {
      try
      {
        if (playerMovementHook != null)
        {
          playerMovementHook?.Disable();
          playerMovementHook?.Dispose();
        }

        if (gposeActorHook != null)
        {
          gposeActorHook?.Disable();
          gposeActorHook?.Dispose();
        }
      }
      catch (Exception ex)
      {
        PluginLog.LogError(ex, "Error while calling PluginMemory.Dispose()");
      }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Vector3
    {
      public float X;
      public float Y;
      public float Z;
    }

    private ConfigModel GetConfigForModelId(short modelId)
    {
      if (ModelCache.ContainsKey(modelId))
      {
        return ModelCache[modelId];
      }
      else
      {
        var config = Plugin.Configuration.Configs.Where(e => e.Model == modelId);
        if (config.Any())
        {
          ModelCache.Add(modelId, config.First());
          return config.First();
        }
        return null;
      }
    }

    private unsafe void PlayerMovementHook(IntPtr player)
    {
      // Call the original function.
      playerMovementHook.Original(player);

      try
      {
        var character = Marshal.PtrToStructure<Character>(player);
        var feet = BitConverter.ToInt16(new byte[2] { character.EquipSlotData[4 * 4], character.EquipSlotData[(4 * 4) + 1] });
        var config = GetConfigForModelId(feet);
        if (config != null)
        {
          SetPosition(config.Offset, player.ToInt64());
        }
      }
      catch (Exception ex)
      {
        PluginLog.LogError(ex, "uh oh");
      }
    }

    private IntPtr GposeActorHook(IntPtr rcx, int rdx, int r8, IntPtr r9, long unknown)
    {
      try
      {
        actor = new IntPtr(Marshal.ReadInt64(r9 + 8));
      }
      catch (Exception ex)
      {
        PluginLog.LogError(ex.Message);
      }

      return gposeActorHook.Original(rcx, rdx, r8, r9, unknown);
    }

    /// <summary>
    /// Sets the position of the Y for given actor.
    /// </summary>
    /// <param name="p_actor">Actor address</param>
    /// <param name="offset">Offset in the Y direction</param>
    public static void SetPosition(float offset, long p_actor = 0)
    {
      try
      {
        var actor = IntPtr.Zero;
        if (p_actor == 0)
          actor = Plugin.ObjectTable[0].Address;
        else
          actor = new IntPtr(p_actor);

        if (actor != IntPtr.Zero)
        {
          var modelPtr = Marshal.ReadInt64(actor, 0xF0);
          if (modelPtr == 0)
            return;
          var positionPtr = new IntPtr(modelPtr + 0x50);
          var position = Marshal.PtrToStructure<Vector3>(positionPtr);

          // Offset the Y coordinate.
          position.Y += offset;

          Marshal.StructureToPtr(position, positionPtr, false);
        }
      }
      catch { }
    }
  }
}