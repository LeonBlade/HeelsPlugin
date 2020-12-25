using Dalamud.Game.Command;
using Dalamud.Plugin;
using System.Globalization;

namespace HeelsPlugin
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Heels Plugin";

        private const string commandName = "/xlheels";

        private DalamudPluginInterface pi;
        private Configuration configuration;
        private PluginMemory memory;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pi = pluginInterface;

            this.configuration = this.pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.configuration.Initialize(this.pi);

            this.memory = new PluginMemory(this.pi);

            this.pi.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Specify a float value to offset your character from the ground. Use 0 or off to disable."
            });
        }

        public void Dispose()
        {
            // Dispose for stuff in Plugin Memory class.
            this.memory.Dispose();

            this.pi.CommandManager.RemoveHandler(commandName);
            this.pi.Dispose();
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
