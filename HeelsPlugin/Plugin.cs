using Dalamud.Game.Command;
using Dalamud.Plugin;
using System.Globalization;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;

namespace HeelsPlugin
{
    public class Plugin : IDalamudPlugin
    {

	    [PluginService] public static CommandManager CommandManager { get; set; } = null!;
	    [PluginService] public static DalamudPluginInterface pi { get; set; } = null!;
	    [PluginService] public static SigScanner SigScanner { get; set; } = null!;
	    [PluginService] public static ObjectTable ObjectTable { get; set; } = null!;

	    public string Name => "Heels Plugin";

        private const string commandName = "/xlheels";

        private Configuration configuration;
        private PluginMemory memory;

        public Plugin()
        {

            this.configuration = pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(pi);

            this.memory = new PluginMemory(pi);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Specify a float value to offset your character from the ground. Use 0 or off to disable."
            });
        }

        public void Dispose()
        {
            // Dispose for stuff in Plugin Memory class.
            this.memory.Dispose();

            CommandManager.RemoveHandler(commandName);
        }

        private void OnCommand(string command, string args)
        {
            var argArr = args.Split(' ');
            if (argArr.Length > 0)
            {
                // Remove the current offset first.
                this.memory.SetPosition(-this.memory.offset);

                if (argArr[0].ToLower() == "off")
                    this.memory.offset = 0;
                else if (float.TryParse(argArr[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float offset))
                    this.memory.offset = offset;

                // Sets the initial offset on command.
                this.memory.SetPosition(this.memory.offset);
            }
        }
    }
}
