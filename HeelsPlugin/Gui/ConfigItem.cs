using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;

namespace HeelsPlugin.Gui
{
  public class ConfigItem
  {
    private readonly int key = 0;
    private readonly ComboWithFilter<Item> combo;
    private readonly ConfigModel config;
    private string ItemName
    {
      get
      {
        if (config == null || (config.ModelMain <= 0 && config.Model <= 0))
          return "";

        var foundItem = combo.Items.Find(item =>
        {
          if (config.ModelMain > 0)
          {
            var thisItem = new EquipItem((uint)item.ModelMain);
            var configItem = new EquipItem(config.ModelMain);

            return thisItem.Main == configItem.Main && thisItem.Variant == configItem.Variant;
          }
          return
            (short)item.ModelMain == config.Model;
        });
        if (foundItem != null)
          return foundItem?.Name;
        return "(Invalid item found)";
      }
    }

    public int Key { get => key; }
    public ConfigModel Model { get => config; }

    public Action<int>? OnDelete;
    public System.Action? OnChange;

    public ConfigItem(int key, ComboWithFilter<Item> combo)
    {
      this.key = key;
      this.combo = combo;

      config = new()
      {
        Name = string.Empty,
        Model = 0,
        Offset = 0f
      };
    }

    public ConfigItem(int key, ComboWithFilter<Item> combo, ConfigModel config)
    {
      this.key = key;
      this.combo = combo;
      this.config = config;
    }

    private Races[] raceValues = Enum.GetValues<Races>();
    private Sexes[] sexValues = Enum.GetValues<Sexes>();

    private Races AllRaces = (Races)255;
    private Sexes AllSexes = (Sexes)255;

    private Races HyurBase = Races.Hyur | Races.Elezen | Races.Miqote | Races.AuRa | Races.Viera;
    private Races RoeBase = Races.Roegadyn | Races.Hrothgar;

    private void DrawModalCheckbox(string label, Races value, Action<bool> onClick)
    {
      DrawModalCheckbox(label, (config.RaceFilter & value) == value, onClick);
    }

    private void DrawModalCheckbox(string label, Sexes value, Action<bool> onClick)
    {
      DrawModalCheckbox(label, (config.SexFilter & value) == value, onClick);
    }

    private void DrawModalCheckbox(string label, bool isValue, Action<bool> onClick)
    {
      if (ImGui.Checkbox(label, ref isValue))
      {
        onClick(isValue);
        OnChange?.Invoke();
      }
    }

    private void DrawFilterModal()
    {
      if (ImGui.BeginPopup($"Filter##{config.GetHashCode()}"))
      {
        ImGui.Text("Races:");

        DrawModalCheckbox("All##Races", AllRaces, (isChecked) => config.RaceFilter = isChecked ? AllRaces : ~AllRaces);

        ImGui.Separator();

        DrawModalCheckbox("Hyur Body", HyurBase, (isChecked) => config.RaceFilter ^= HyurBase);
        DrawModalCheckbox("Roegadyn Body", RoeBase, (isChecked) => config.RaceFilter ^= RoeBase);

        ImGui.Separator();

        // iterate over races to form UI
        for (var i = 0; i < raceValues.Length; i++)
          DrawModalCheckbox(raceValues[i].ToString(), raceValues[i], (isChecked) => config.RaceFilter ^= raceValues[i]);

        ImGui.Separator();

        ImGui.Text("Sexes:");

        var isSexesAll = (config.SexFilter & AllSexes) == AllSexes;
        if (ImGui.Checkbox("All##Sexes", ref isSexesAll))
        {
          config.SexFilter = isSexesAll ? AllSexes : ~AllSexes;
          OnChange?.Invoke();
        }

        ImGui.Separator();

        for (var i = 0; i < sexValues.Length; i++)
          DrawModalCheckbox(sexValues[i].ToString(), sexValues[i], (isChecked) => config.SexFilter ^= sexValues[i]);

        ImGui.Spacing();

        if (ImGui.Button("Close"))
          ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
      }
    }

    public void Draw()
    {
      try
      {
        var fontScale = ImGui.GetIO().FontGlobalScale;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        // Enabled
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12 * fontScale));
        if (ImGui.Checkbox($"##Enabled{key}", ref config.Enabled))
          OnChange?.Invoke();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enable or disable this entry from being used");

        // Name
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.InputText($"##Name{key}", ref config.Name, 64))
          OnChange?.Invoke();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set a name to remember what this entry is used for");

        // Item
        ImGui.TableNextColumn();
        ImGui.BeginGroup();

        // Eyedrop button
        if (ImGuiComponents.IconButton(Key, FontAwesomeIcon.EyeDropper))
        {
          var feetPics = Plugin.Memory.GetPlayerFeet();
          config.ModelMain = feetPics.ToUInt();
          OnChange?.Invoke();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set as your active footwear");

        // Item
        ImGui.SameLine();
        if (combo.Draw($"{ItemName}##{key}", out var item, -1))
        {
          config.ModelMain = (uint)item.ModelMain;
          OnChange?.Invoke();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Select an item for this entry");
        ImGui.EndGroup();
        ImGui.PopItemWidth();

        // Filter
        ImGui.TableNextColumn();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (4 * fontScale));
        if (ImGuiComponents.IconButton(Key, FontAwesomeIcon.Filter))
          ImGui.OpenPopup($"Filter##{config.GetHashCode()}");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set a filter for who this config works for");

        DrawFilterModal();

        // Height
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(110 * fontScale);
        if (ImGui.InputFloat($"##Height{key}", ref config.Offset, 0.01f, 0.05f, "%.3f"))
          OnChange?.Invoke();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set how much the heels add to your height");

        // Delete
        ImGui.TableNextColumn();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (11 * fontScale));
        if (ImGuiComponents.IconButton(Key, FontAwesomeIcon.TrashAlt))
        {
          config.Enabled = false;
          OnDelete?.Invoke(key);
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Deletes an entry from your config");
      }
      finally
      {
      }
    }
  }
}
