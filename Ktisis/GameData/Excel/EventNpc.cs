using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using Ktisis.Common.Extensions;
using Ktisis.GameData.Excel.Types;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("ENpcBase", columnHash: 0x5ba9e1a6)]
public struct EventNpc(uint row) : IExcelRow<EventNpc>, INpcBase {
	public uint RowId { get; } = row;

	public RowRef<ModelChara> ModelChara { get; init; }
	public CustomizeContainer Customize { get; init; }
	public WeaponModelId MainHand { get; init; }
	public WeaponModelId OffHand { get; init; }
	public EquipmentContainer Equipment { get; init; }

	static EventNpc IExcelRow<EventNpc>.Create(ExcelPage page, uint offset, uint row) {
		return new EventNpc(row) {
			Name = $"E:{row:D7}",
			ModelChara = page.ReadRowRef<ModelChara>(35, offset),
			Customize = page.ReadCustomize(36, offset),
			MainHand = page.ReadWeapon(65, offset),
			OffHand = page.ReadWeapon(68, offset),
			Equipment = ReadEquipment(page, offset)
		};
	}

	private static EquipmentContainer ReadEquipment(ExcelPage page, uint offset) {
		var equipRow = page.ReadColumn<ushort>(63, offset);
		var equip = page.ReadEquipment(71, offset);

		if (equipRow is 0 or 175) return equip;

		var altEquip = new RowRef<NpcEquipment>(page.Module, equipRow, page.Language);
		if (!altEquip.IsValid) return equip;
		
		for (uint i = 0; i < EquipmentContainer.Length; i++) {
			var altVal = altEquip.Value.Equipment[i];
			if (!altVal.Equals(default))
				equip[i] = altVal;
		}

		return equip;
	}
	
	// INpcBase
	
	public string Name { get; set; } = string.Empty;

	public ushort GetModelId() => (ushort)this.ModelChara.RowId;

	public CustomizeContainer? GetCustomize() => this.Customize;
	public EquipmentContainer GetEquipment() => this.Equipment;

	public WeaponModelId? GetMainHand() => this.MainHand;
	public WeaponModelId? GetOffHand() => this.OffHand;
}
