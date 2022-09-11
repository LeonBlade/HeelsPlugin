using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;

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
        HelpMessage = "Specify a float value to offset your character from the ground. Use 0 or off to disable. No arguments will provide a config menu.",
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
