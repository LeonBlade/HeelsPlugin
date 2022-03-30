using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HeelsPlugin.Gui
{
  public class ConfigLine
  {
    private readonly int key = 0;
    private readonly ComboWithFilter<Item> combo;
    private readonly ConfigModel model;
    private string itemName
    {
      get
      {
        if (model == null || (model.ModelMain <= 0 && model.Model <= 0)) return "";
        return combo.Items.Find(c =>
        {
          if (model.ModelMain > 0) return c.ModelMain == model.ModelMain;
          return (short)c.ModelMain == model.Model;
        }).Name.ToString();
      }
    }

    public int Key { get => key; }
    public ConfigModel Model { get => model; }

    public System.Action<int>? OnDelete;
    public System.Action? OnChange;

    public ConfigLine(int key, ComboWithFilter<Item> combo)
    {
      this.key = key;
      this.combo = combo;

      model = new()
      {
        Name = string.Empty,
        Model = 0,
        Offset = 0f
      };
    }

    public ConfigLine(int key, ComboWithFilter<Item> combo, ConfigModel model)
    {
      this.key = key;
      this.combo = combo;
      this.model = model;
    }

    private void UpdatePlayerY()
    {
      if (model.Enabled && Plugin.Memory.GetPlayerFeet().ToUlong() == model.ModelMain)
        Plugin.Memory.SetPosition(Plugin.Memory.PlayerY + model.Offset, 0, true);
      else if (!model.Enabled && Plugin.Memory.GetPlayerFeet().ToUlong() == model.ModelMain)
        RestorePlayerY();
    }

    private void RestorePlayerY()
    {
      Plugin.Memory.SetPosition(Plugin.Memory.PlayerY, 0, true);
    }

    public void Draw()
    {
      try
      {
        var fontScale = ImGui.GetIO().FontGlobalScale;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12 * fontScale));
        if (ImGui.Checkbox($"##Enabled{key}", ref model.Enabled))
        {
          OnChange?.Invoke();
          UpdatePlayerY();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enable or disable this entry from being used");

        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.InputText($"##Name{key}", ref model.Name, 64))
          OnChange?.Invoke();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set a name to remember what this entry is used for");

        ImGui.TableNextColumn();

        ImGui.BeginGroup();
        if (ImGuiComponents.IconButton(Key, FontAwesomeIcon.EyeDropper))
        {
          var feetPics = Plugin.Memory.GetPlayerFeet();
          model.ModelMain = feetPics.ToUlong();
          OnChange?.Invoke();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set as your active footwear");

        ImGui.SameLine();
        if (combo.Draw($"{itemName}##{key}", out var item, -1))
        {
          model.ModelMain = item.ModelMain;
          OnChange?.Invoke();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Select an item for this entry");
        ImGui.EndGroup();
        ImGui.PopItemWidth();

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(110 * fontScale);
        if (ImGui.InputFloat($"##Height{key}", ref model.Offset, 0.01f, 0.01f, "%.2f"))
        {
          OnChange?.Invoke();
          UpdatePlayerY();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Set how much the heels add to your height");

        ImGui.TableNextColumn();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (11 * fontScale));
        if (ImGuiComponents.IconButton(Key, FontAwesomeIcon.TrashAlt))
        {
          model.Enabled = false;
          OnDelete?.Invoke(key);
          RestorePlayerY();
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Deletes an entry from your config");
      }
      finally
      {
      }
    }
  }
}
