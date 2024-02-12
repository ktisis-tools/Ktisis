using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

using Ktisis.Common.Extensions;
using Ktisis.GameData.Excel.Types;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("ENpcBase", columnHash: 0x927347d8)]
public class EventNpc : ExcelRow, INpcBase {
	public LazyRow<ModelChara> ModelChara { get; private set; } = null!;

	public CustomizeContainer Customize { get; private set; }
	public WeaponModelId MainHand { get; private set; }
	public WeaponModelId OffHand { get; private set; }
	public EquipmentContainer Equipment { get; private set; }

	public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
		base.PopulateData(parser, gameData, language);

		this.Name = $"E:{this.RowId:D7}";
		
		this.ModelChara = new LazyRow<ModelChara>(gameData, parser.ReadColumn<ushort>(35), language);

		var equipRow = parser.ReadColumn<ushort>(63);
		
		this.MainHand = parser.ReadWeapon(65);
		this.OffHand = parser.ReadWeapon(67);
		var equip = this.Equipment = parser.ReadEquipment(69);
		
		// what the fuck?
		
		var altEquip = equipRow is not (0 or 175) ? new LazyRow<NpcEquipment>(gameData, equipRow, language) : null;
		if (altEquip?.Value == null) return;

		for (uint i = 0; i < EquipmentContainer.Length; i++) {
			var altVal = altEquip.Value.Equipment[i];
			if (!altVal.Equals(default))
				equip[i] = altVal;
		}
	}
	
	// INpcBase
	
	public string Name { get; set; } = string.Empty;

	public ushort GetModelId() => (ushort)this.ModelChara.Row;

	public CustomizeContainer? GetCustomize() => this.Customize;
	public EquipmentContainer GetEquipment() => this.Equipment;

	public WeaponModelId? GetMainHand() => this.MainHand;
	public WeaponModelId? GetOffHand() => this.OffHand;
}
