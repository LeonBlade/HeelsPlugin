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

    public float? playerY = null;
    public float PlayerY
    {
      get
      {
        if (playerY == null)
        {
          var (_, position) = GetActorPosition(IntPtr.Zero);
          playerY = position.Y;
          return (float)playerY;
        }
        return (float)playerY;
      }
      set => playerY = value;
    }

    public PluginMemory()
    {
      playerMovementFunc = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 03 48 8B CB FF 50 18 83 F8 02 75 ??");
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

    private ConfigModel GetConfigForModelId(ulong modelId)
    {
      var config = Plugin.Configuration.Configs.Where(e =>
      {
        var valid = false;
        if (e.ModelMain > 0)
        {
          var qModelMain = new Quad(e.ModelMain);
          var qModelId = new Quad(modelId);

          valid = qModelMain.A == qModelId.A && (byte)qModelMain.B == (byte)qModelId.B;
        }
        else valid = e.Model == (short)modelId;
        return valid && e.Enabled;
      });
      if (config.Any())
      {
        return config.First();
      }
      return null;
    }

    public Quad GetPlayerFeet()
    {
      var player = Plugin.ObjectTable[0].Address;
      return GetPlayerFeet(player);
    }

    public Quad GetPlayerFeet(IntPtr player)
    {
      var feet = (ulong)Marshal.ReadInt32(player + 0xDC0);
      var feetPics = new Quad(feet);
      PluginLog.Log($"feet {feet:X}, pics {feetPics.ToUlong():X}");
      return feetPics;
    }

    private unsafe void PlayerMovementHook(IntPtr player)
    {
      // Call the original function.
      playerMovementHook.Original(player);

      try
      {
        if (player == Plugin.ObjectTable[0].Address)
        {
          var (_, position) = GetActorPosition(player);
          PlayerY = position.Y;
        }

        var feet = (ulong)Marshal.ReadInt32(player + 0xDC0);
        var config = GetConfigForModelId(feet);
        if (config != null && config.Enabled)
        {
          SetPosition(config.Offset, player.ToInt64());
        }
      }
      catch (Exception ex)
      {
        PluginLog.LogError(ex, $"Error while moving with player {player.ToInt64():X}");
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

    private (IntPtr, Vector3) GetActorPosition(IntPtr actor)
    {
      try
      {
        var modelPtr = Marshal.ReadInt64(actor, 0xF0);
        if (modelPtr == 0)
          return (IntPtr.Zero, Vector3.Zero);
        var positionPtr = new IntPtr(modelPtr + 0x50);
        return (positionPtr, Marshal.PtrToStructure<Vector3>(positionPtr));
      }
      catch
      {
        return (IntPtr.Zero, Vector3.Zero);
      }
    }

    /// <summary>
    /// Sets the position of the Y for given actor.
    /// </summary>
    /// <param name="actorAddress">Actor address</param>
    /// <param name="offset">Offset in the Y direction</param>
    public void SetPosition(float offset, long actorAddress = 0, bool replace = false)
    {
      try
      {
        var actor = IntPtr.Zero;
        if (actorAddress == 0)
          actor = Plugin.ObjectTable[0].Address;
        else
          actor = new IntPtr(actorAddress);

        if (actor != IntPtr.Zero)
        {
          var (positionPtr, position) = GetActorPosition(actor);
          if (positionPtr == IntPtr.Zero) return;

          // Offset the Y coordinate.
          if (replace) position.Y = offset;
          else position.Y += offset;

          Marshal.StructureToPtr(position, positionPtr, false);
        }
      }
      catch { }
    }
  }
}