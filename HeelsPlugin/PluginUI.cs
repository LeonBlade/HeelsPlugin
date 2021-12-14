using Dalamud.Logging;
using HeelsPlugin.Gui;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HeelsPlugin
{
  public class PluginUI
  {
    private bool visible = false;
    public bool Visible
    {
      get => visible;
      set => visible = value;
    }
    private readonly IReadOnlyDictionary<uint, Item> feet;
    private readonly Dictionary<int, ConfigLine> configs = new();
    private int index = -1;

    private const int MIN_MAX_WIDTH = 600;

    public PluginUI()
    {
      feet = GameData.Equipment(Plugin.Data);

      try
      {
        var configs = Plugin.Configuration.Configs;

        if (configs.Count > 0)
        {
          for (var i = 0; i < configs.Count; i++)
          {
            AddConfigLine(configs[i]);
          }
        }
        else
        {
          AddConfigLine();
        }
      }
      catch (Exception ex)
      {
        PluginLog.Error(ex, "Failed to create PluginUI");
      }
    }

    public void Draw()
    {
      try
      {
        DrawConfiguration();
      }
      catch
      {
      }
    }

    private ComboWithFilter<Item> CreateCombo(int id)
    {
      return new($"##FeetPics{id}", 300f, 200f, feet.Values.ToArray(), e => e.Name.ToString())
      {
        Flags = ImGuiComboFlags.HeightLarge
      };
    }

    private void AddConfigLine(ConfigModel? model = null)
    {
      index++;
      ConfigLine line;
      if (model == null)
        line = new(index, CreateCombo(index));
      else
        line = new(index, CreateCombo(index), model);
      line.OnDelete += HandleDelete;
      line.OnChange += HandleChange;
      configs.Add(index, line);
    }

    private void HandleDelete(int key)
    {
      configs.Remove(key);
    }

    private void HandleChange()
    {
      Plugin.Configuration.Configs = configs.Values.Select(c => c.Model).ToList();
      Plugin.Configuration.Save();
    }

    public void DrawConfiguration()
    {
      if (!visible) return;

      var fontScale = ImGui.GetIO().FontGlobalScale;
      var size = new Vector2(MIN_MAX_WIDTH * fontScale, 200 * fontScale);

      ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
      ImGui.SetNextWindowSizeConstraints(size, new Vector2(MIN_MAX_WIDTH * fontScale, 900 * fontScale));

      if (ImGui.Begin("HeelsPlugin Config", ref visible))
      {
        ImGui.PushItemWidth(MIN_MAX_WIDTH * fontScale);
        if (ImGui.Button("Add Config"))
        {
          AddConfigLine();
        }
        ImGui.PopItemWidth();

        foreach (var config in configs)
        {
          config.Value.Draw();
        }
      }
    }
  }
}
