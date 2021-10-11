using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace HeelsPlugin
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public int Version { get; set; } = 1;

    public List<ConfigModel> Configs = new();

    public void Save()
    {
      Plugin.PluginInterface.SavePluginConfig(this);
    }
  }
}
