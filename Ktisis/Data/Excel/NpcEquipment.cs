using Lumina.Excel;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
    [Sheet("NpcEquip", columnHash: 0x4004F596)]
	public struct NpcEquipment(uint row) : IExcelRow<NpcEquipment> {
		public uint RowId => row;
		
		public WeaponEquip MainHand { get; private set; }
		public WeaponEquip OffHand { get; private set; }

		public Equipment Equipment { get; private set; }
		
		public static NpcEquipment Create(ExcelPage page, uint offset, uint row) {
			return new NpcEquipment(row) {
				MainHand = page.ReadWeapon(0, offset),
				OffHand = page.ReadWeapon(3, offset),
				Equipment = page.ReadEquipment(6, offset)
			};
		}
	}
}
