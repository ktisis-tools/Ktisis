using Lumina.Excel;
using Lumina.Excel.Sheets;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("BNpcBase", columnHash: 0xB8CBAD27)]
	public struct BattleNpc(uint row) : IExcelRow<BattleNpc>, INpcBase {
		// Excel

		public uint RowId => row;
        
		public float Scale { get; set; }
		public RowRef<ModelChara> ModelChara { get; set; }
		private RowRef<BNpcCustomizeSheet> CustomizeSheet { get; set; }
		private RowRef<NpcEquipment> NpcEquipment { get; set; }
		
		public static BattleNpc Create(ExcelPage page, uint offset, uint row) {
			return new BattleNpc(row) {
				Scale = page.ReadColumn<float>(4, offset),
				ModelChara = page.ReadRowRef<ModelChara>(5, offset),
				CustomizeSheet = page.ReadRowRef<BNpcCustomizeSheet>(6, offset),
				NpcEquipment = page.ReadRowRef<NpcEquipment>(7, offset)
			};
		}
		
		// INpcBase

		public string Name { get; set; } = string.Empty;

		public ushort GetModelId()
			=> (ushort)this.ModelChara.RowId;
		
		public Customize? GetCustomize()
			=> this.CustomizeSheet.RowId != 0 ? this.CustomizeSheet.Value.Customize : null;

		public Equipment? GetEquipment()
			=> this.NpcEquipment.Value.Equipment;

		public WeaponEquip? GetMainHand()
			=> this.NpcEquipment.Value.MainHand;

		public WeaponEquip? GetOffHand()
			=> this.NpcEquipment.Value.OffHand;
		
		// Customize Sheet
		
		[Sheet("BNpcCustomize", columnHash: 0x18f060d4)]
		private struct BNpcCustomizeSheet(uint row) : IExcelRow<BNpcCustomizeSheet> {
			public uint RowId => row;
			
			public Customize Customize { get; set; }
			
			public static BNpcCustomizeSheet Create(ExcelPage page, uint offset, uint row) {
				return new BNpcCustomizeSheet(row) {
					Customize = page.ReadCustomize(0, offset)
				};
			}
		}
	}
}
