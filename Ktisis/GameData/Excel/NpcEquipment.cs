using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Data;
using Lumina.Excel;

using Ktisis.Common.Extensions;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("NpcEquip", columnHash: 0xe91c87ba)]
public class NpcEquipment : ExcelRow {
	public WeaponModelId MainHand { get; private set; }
	public WeaponModelId OffHand { get; private set; }
	
	public EquipmentContainer Equipment { get; set; }

	public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
		base.PopulateData(parser, gameData, language);

		this.MainHand = parser.ReadWeapon(0);
		this.OffHand = parser.ReadWeapon(2);
		this.Equipment = parser.ReadEquipment(4);
	}
}
