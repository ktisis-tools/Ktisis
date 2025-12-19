using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel;

using Ktisis.Common.Extensions;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("NpcEquip", columnHash: 0x7eaeb95c)]
public struct NpcEquipment(ExcelPage page, uint offset, uint row) : IExcelRow<NpcEquipment> {
	public ExcelPage ExcelPage => page;
	public uint RowOffset { get; } = offset;
	public uint RowId { get; } = row;

	public WeaponModelId MainHand { get; private set; }
	public WeaponModelId OffHand { get; private set; }
	
	public EquipmentContainer Equipment { get; set; }

	static NpcEquipment IExcelRow<NpcEquipment>.Create(ExcelPage page, uint offset, uint row) {
		return new NpcEquipment(page, offset, row) {
			MainHand = page.ReadWeapon(0, offset),
			OffHand = page.ReadWeapon(3, offset),
			Equipment = page.ReadEquipment(6, offset)
		};
	}
}
