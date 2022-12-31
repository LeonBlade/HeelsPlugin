using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Gui;

namespace HeelsPlugin
{
  public class Plugin : IDalamudPlugin
  {

    [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static SigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static ObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static DataManager Data { get; private set; } = null!;
    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    [PluginService] public static ChatGui ChatGui { get; private set; } = null!;

    public string Name => "Heels Plugin";

    private const string commandName = "/xlheels";

    public static Configuration Configuration = null!;
    public static PluginMemory Memory = null!;
    public static IpcManager Ipc = null!;
    private readonly PluginUI ui;

    public Plugin()
    {
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Memory = new();
      Ipc = new(PluginInterface, Memory);
      ui = new();


      CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
      {
        HelpMessage = $"Open the {Name} configuration window. Use '{commandName} help' for more options.",
        ShowInHelp = true
      });

      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
      ClientState.TerritoryChanged += (_, __) => Memory.PlayerOffsets.Clear();
    }

    public void Dispose()
    {
      // Dispose for stuff in Plugin Memory class.
      Memory?.Dispose();
      Ipc?.Dispose();

      CommandManager.RemoveHandler(commandName);

      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

      GC.SuppressFinalize(this);
    }

    private void OnCommand(string command, string args)
    {
      var arg = Regex.Matches(args, @"[\""].+?[\""]|[^ ]+").Select(m =>
      {
        if (m.Value.StartsWith('"') && m.Value.EndsWith('"'))
        {
          return m.Value.Substring(1, m.Value.Length - 2);
        }

        return m.Value;
      }).ToArray();
      
      void ApplyAction(string nameCondition, Action<ConfigModel> action)
      {
        var c = 0;
        if (nameCondition.StartsWith('/') && nameCondition.EndsWith('/'))
        {
          var regexStr = nameCondition.Substring(1, nameCondition.Length - 2);
          var regex = new Regex(regexStr);
          foreach (var e in Configuration.Configs.Where(e => regex.IsMatch(e.Name)))
          {
            action(e);
            c++;
          }
        }
        else
        {
          foreach (var e in Configuration.Configs.Where(e => e.Name == nameCondition))
          {
            action(e);
            c++;
          }
        }

        if (c == 0)
          ChatGui.PrintError($"[{Name}] No config entries found matching '{nameCondition}'.");
        else {
          Configuration.Save();
          Memory.RestorePlayerY();
        }
      }
      
      if (arg.Length > 0)
      {
        switch (arg[0].ToLowerInvariant())
        {
          case "config": break;
          case "toggle" when arg.Length >= 2:
            ApplyAction(arg[1], c => c.Enabled = !c.Enabled);
            return;
          case "enable" when arg.Length >= 2:
            ApplyAction(arg[1], c => c.Enabled = true);
            return;
          case "disable" when arg.Length >= 2:
            ApplyAction(arg[1], c => c.Enabled = false);
            return;
          default:
            ChatGui.Print($"{commandName} - Open the config window.");
            ChatGui.Print($"{commandName} toggle [name] - Toggle the config entry with the given name.");
            ChatGui.Print($"{commandName} enable [name] - Enable the config entry with the given name.");
            ChatGui.Print($"{commandName} disable [name] - Disable the config entry with the given name.");
            return;
        }
      }
      
      ui.Visible = !ui.Visible;
    }

    private void OpenConfig()
    {
      ui.Visible = true;
    }

    private void DrawUI()
    {
      ui.Draw();
    }
  }
}
