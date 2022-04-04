using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeelsPlugin
{
  public class PluginMemory
  {
    public IntPtr playerMovementFunc;
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void PlayerMovementDelegate(IntPtr player);
    public readonly Hook<PlayerMovementDelegate> playerMovementHook;

    public PluginMemory()
    {
      playerMovementFunc = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 03 48 8B CB FF 50 18 83 F8 02 75 ??");
      playerMovementHook = new Hook<PlayerMovementDelegate>(
        playerMovementFunc,
        new PlayerMovementDelegate(PlayerMovementHook)
      );

      playerMovementHook.Enable();
    }

    public void Dispose()
    {
      try
      {
        if (playerMovementHook != null)
        {
          playerMovementHook?.Disable();
          playerMovementHook?.Dispose();
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

    public void RestorePlayerY()
    {
      SetPosition(Plugin.ObjectTable[0].Position.Y, 0, true);
      PlayerMove(Plugin.ObjectTable[0].Address);
    }

    public Quad GetPlayerFeet()
    {
      var player = Plugin.ObjectTable[0].Address;
      return GetPlayerFeet(player);
    }

    public Quad GetPlayerFeet(IntPtr player)
    {
      var feet = (ulong)Marshal.ReadInt64(player + 0xDC0);
      return new Quad(feet);
    }

    private bool IsConfigValidForActor(IntPtr player, ConfigModel config)
    {
      // create game object from pointer
      var gameObject = Plugin.ObjectTable.CreateObjectReference(player);
      if (gameObject.ObjectKind != ObjectKind.Player)
        return false;

      // get a character from the game object
      var character = (Character)gameObject;

      // get the race and sex of character for filtering on config
      var race = (Races)Math.Pow(character.Customize[(int)CustomizeIndex.Race], 2);
      var sex = (Sexes)character.Customize[(int)CustomizeIndex.Gender] + 1;

      if (config != null && config.Enabled && ((config.RaceFilter & race) == race) && ((config.SexFilter & sex) == sex))
        return true;

      return false;
    }

    public unsafe void PlayerMove(IntPtr player)
    {
      try
      {
        // get the feet gear
        var feet = (ulong)Marshal.ReadInt32(player + 0xDC0);
        var config = GetConfigForModelId(feet);

        // Check config and set position
        if (IsConfigValidForActor(player, config))
          SetPosition(config.Offset, player.ToInt64());
      }
      catch (Exception ex)
      {
        PluginLog.LogError(ex, $"Error while moving with player {player.ToInt64():X}");
      }
    }

    private unsafe void PlayerMovementHook(IntPtr player)
    {
      // Call the original function.
      playerMovementHook.Original(player);
      PlayerMove(player);
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