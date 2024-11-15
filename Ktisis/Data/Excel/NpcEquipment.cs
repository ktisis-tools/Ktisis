using Lumina.Excel;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
    [Sheet("NpcEquip", columnHash: 0xe91c87ba)]
	public struct NpcEquipment(uint row) : IExcelRow<NpcEquipment> {
		public uint RowId => row;
		
		public WeaponEquip MainHand { get; private set; }
		public WeaponEquip OffHand { get; private set; }

		public Equipment Equipment { get; private set; }
		
		public static NpcEquipment Create(ExcelPage page, uint offset, uint row) {
			return new NpcEquipment(row) {
				MainHand = page.ReadWeapon(0),
				OffHand = page.ReadWeapon(3),
				Equipment = page.ReadEquipment(6)
			};
		}
	}
}
