using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
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
    public Dictionary<GameObject, float> PlayerOffsets = new();

    private float? lastOffset = null;

    private GameObject PlayerSelf => Plugin.ObjectTable.First();

    public PluginMemory()
    {
      playerMovementFunc = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B 03 48 8B CB FF 50 ?? 83 F8 ?? 75 ??");
      playerMovementHook = Hook<PlayerMovementDelegate>.FromAddress(playerMovementFunc, new PlayerMovementDelegate(PlayerMovementHook));

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
      catch
      {
      }
    }

    private ConfigModel? GetConfig(EquipItem inModel)
    {
      var foundConfig = Plugin.Configuration?.Configs.Where(config =>
      {
        var valid = false;
        if (config.ModelMain > 0)
        {
          var configModel = new EquipItem(config.ModelMain);
          valid = configModel.Main == inModel.Main && configModel.Variant == inModel.Variant;
        }
        else
          valid = config.Model == inModel.Main;

        return valid && config.Enabled;
      });

      // return the last one in the list
      if (foundConfig != null && foundConfig.Any())
        return foundConfig.Last();

      return null;
    }

    private ConfigModel? GetConfig(IntPtr addr)
    {
      var feet = GetPlayerFeet(addr);
      if (!feet.HasValue)
        return null;
      return GetConfig(feet.Value);
    }

    public void RestorePlayerY()
    {
      var player = PlayerSelf;
      if (player != null)
      {
        SetPosition(player.Position.Y, player.Address, true);
        PlayerMove(player.Address);
      }
    }

    public EquipItem? GetPlayerFeetItem()
    {
      var player = PlayerSelf.Address;
      return GetPlayerFeet(player);
    }

    public EquipItem? GetPlayerFeet(IntPtr? player)
    {
      if (!player.HasValue)
        return null;

      var feet = (uint)Marshal.ReadInt32(player.Value + 0x818 + 0x10);
      return new EquipItem(feet);
    }

    private bool IsConfigValidForActor(IntPtr player, ConfigModel? config)
    {
      // create game object from pointer
      var gameObject = Plugin.ObjectTable.CreateObjectReference(player);
      var character = CharacterFactory.Convert(gameObject);

      if (character == null || character.ModelType() != 0)
        return false;

      // get the race and sex of character for filtering on config
      var race = (Races)Math.Pow(character.Customize[(int)CustomizeIndex.Race], 2);
      var sex = (Sexes)character.Customize[(int)CustomizeIndex.Gender] + 1;

      var containsRace = (config?.RaceFilter & race) == race;
      var containsSex = (config?.SexFilter & sex) == sex;

      if (config != null && config.Enabled && containsRace && containsSex)
        return true;

      return false;
    }

    public float GetPlayerOffset()
    {
      var feet = GetPlayerFeetItem();
      if (!feet.HasValue)
        return 0;
      var config = GetConfig(feet.Value);

      return config?.Offset ?? 0;
    }

    public unsafe void PlayerMove(IntPtr player)
    {
      try
      {
        if (player == PlayerSelf.Address)
        {
          ProcessSelf();
          goto processPlayer;
        }
        else
        {
          var playerObject = Plugin.ObjectTable.CreateObjectReference(player);

          // check against dictionary created from IPC
          if (playerObject != null && PlayerOffsets.ContainsKey(playerObject))
          {
            SetPosition(PlayerOffsets[playerObject], player);
          }
          else
          {
            goto processPlayer;
          }
        }
        return;

      processPlayer:
        {
          var config = GetConfig(player);
          if (config != null && IsConfigValidForActor(player, config))
            SetPosition(config.Offset, player);
        }
      }
      catch
      {
      }
    }

    private void ProcessSelf()
    {
      var config = GetConfig(PlayerSelf.Address);
      if (lastOffset != config?.Offset && config?.Offset != null)
        Plugin.Ipc?.OnOffsetChange(config.Offset);
      lastOffset = config?.Offset;
    }

    private unsafe void PlayerMovementHook(IntPtr player)
    {
      // Call the original function.
      playerMovementHook.Original(player);
      PlayerMove(player);
    }

    private (IntPtr, Vector3) GetPosition(IntPtr actor)
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

    public void SetPosition(float offset, IntPtr actor, bool replace = false)
    {
      try
      {
        if (actor != IntPtr.Zero)
        {
          var (positionPtr, position) = GetPosition(actor);
          if (positionPtr == IntPtr.Zero)
            return;

          // Offset the Y coordinate.
          if (replace)
            position.Y = offset;
          else
            position.Y += offset;

          Marshal.StructureToPtr(position, positionPtr, false);
        }
      }
      catch { }
    }
  }
}