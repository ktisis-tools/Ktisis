using Lumina.Excel;

using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
    [Sheet("NpcEquip", columnHash: 0x7EAEB95C)]
	public struct NpcEquipment(ExcelPage page, uint offset, uint row) : IExcelRow<NpcEquipment> {
		public ExcelPage ExcelPage => page;
		public uint RowOffset => offset;
		public uint RowId => row;
		
		public WeaponEquip MainHand { get; private set; }
		public WeaponEquip OffHand { get; private set; }

		public Equipment Equipment { get; private set; }
		
		public static NpcEquipment Create(ExcelPage page, uint offset, uint row) {
			return new NpcEquipment(page, offset, row) {
				MainHand = page.ReadWeapon(0, offset),
				OffHand = page.ReadWeapon(3, offset),
				Equipment = page.ReadEquipment(6, offset)
			};
		}
	}
}
