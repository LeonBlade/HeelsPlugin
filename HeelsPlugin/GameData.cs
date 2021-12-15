using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using Penumbra.GameData.Enums;
using System.Collections.Generic;
using System.Linq;

namespace HeelsPlugin
{
  public class GameData
  {
    private static Dictionary<uint, Item>? equipment;

    public static IReadOnlyDictionary<uint, Item> Equipment(DataManager dataManager)
    {
      if (equipment != null)
        return equipment;

      var equipmentSheet = dataManager.GetExcelSheet<Item>()!;
      var equipSlotCategorySheet = dataManager.GetExcelSheet<EquipSlotCategory>()!;
      equipment = equipmentSheet.Where(e => e.EquipSlotCategory.Row == (uint)EquipSlot.Feet).ToDictionary(e => e.RowId, e => e);
      return equipment;
    }
  }
}
