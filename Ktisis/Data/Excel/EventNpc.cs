using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("ENpcBase", columnHash: 0x927347d8)]
	public class EventNpc : ExcelRow, INpcBase {
		// Excel

		public ushort EventHandler { get; set; }

		public LazyRow<ModelChara> ModelChara { get; set; } = null!;
		public Customize Customize { get; set; }

		public WeaponEquip MainHand { get; set; }
		public WeaponEquip OffHand { get; set; }
		public Equipment Equipment { get; set; }

		public override void PopulateData(RowParser parser, GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			this.Name = $"E:{this.RowId:D7}";

			this.EventHandler = parser.ReadColumn<ushort>(0);

			this.ModelChara = new LazyRow<ModelChara>(gameData, parser.ReadColumn<ushort>(35), language);
			this.Customize = parser.ReadCustomize(36);

			var equipRow = parser.ReadColumn<ushort>(63);

			this.MainHand = parser.ReadWeapon(65);
			this.OffHand = parser.ReadWeapon(68);
            this.Equipment = parser.ReadEquipment(71);

			// what the fuck?
			var equip = equipRow is not (0 or 175) ? new LazyRow<NpcEquipment>(gameData, equipRow, language) : null;
			if (equip?.Value == null) return;

			this.Equipment = EquipOverride(this.Equipment, equip.Value.Equipment);
		}

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

		public ushort GetModelId() => (ushort)this.ModelChara.Row;

		public Customize? GetCustomize() => this.Customize;

		public Equipment? GetEquipment() => this.Equipment;

		public WeaponEquip? GetMainHand() => this.MainHand;

		public WeaponEquip? GetOffHand() => this.OffHand;
	}
}
