using Lumina.Excel;
using Lumina.Excel.Sheets;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("ENpcBase", columnHash: 0x464052CD)]
	public struct EventNpc : IExcelRow<EventNpc>, INpcBase {
		// Excel
		
		public uint RowId { get; }

		public ushort EventHandler { get; set; }

		public RowRef<ModelChara> ModelChara { get; set; }
		public Customize Customize { get; set; }

		public WeaponEquip MainHand { get; set; }
		public WeaponEquip OffHand { get; set; }
		public Equipment Equipment { get; set; }

		public EventNpc(ExcelPage page, uint offset, uint row) {
			this.RowId = row;

			this.Name = $"E:{this.RowId:D7}";

			this.EventHandler = page.ReadColumn<ushort>(0, offset);

			this.ModelChara = page.ReadRowRef<ModelChara>(35, offset);
			this.Customize = page.ReadCustomize(36, offset);

			var equipRow = page.ReadColumn<ushort>(63, offset);

			this.MainHand = page.ReadWeapon(65, offset);
			this.OffHand = page.ReadWeapon(68, offset);
			this.Equipment = page.ReadEquipment(71, offset);
			
			// what the fuck?
			
			if (equipRow is 0 or 175) return;
			
			var equip = new RowRef<NpcEquipment>(page.Module, equipRow, page.Language);
			this.Equipment = EquipOverride(this.Equipment, equip.Value.Equipment);
		}

		public static EventNpc Create(ExcelPage page, uint offset, uint row) => new(page, offset, row);

		private unsafe Equipment EquipOverride(Equipment equip, Equipment alt) {
			for (var i = 0; i < Equipment.SlotCount; i++) {
				var altVal = alt.Slots[i];
				if (altVal != 0)
					equip.Slots[i] = altVal;
			}
			
			return equip;
		}

		// INpcBase
		
		public string Name { get; set; } = null!;

		public ushort GetModelId() => (ushort)this.ModelChara.RowId;

		public Customize? GetCustomize() => this.Customize;

		public Equipment? GetEquipment() => this.Equipment;

		public WeaponEquip? GetMainHand() => this.MainHand;

		public WeaponEquip? GetOffHand() => this.OffHand;
	}
}
