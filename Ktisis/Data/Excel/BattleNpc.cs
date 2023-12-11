using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("BNpcBase", columnHash: 0xe136dda3)]
	public class BattleNpc : ExcelRow, INpcBase {
		// Excel
        
		public float Scale { get; set; }
		public LazyRow<ModelChara> ModelChara { get; set; } = null!;
		private LazyRow<BNpcCustomizeSheet> CustomizeSheet { get; set; } = null!;
		private LazyRow<NpcEquipment> NpcEquipment { get; set; } = null!;

		public override void PopulateData(RowParser parser, GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);
            
			this.Scale = parser.ReadColumn<float>(4);
			this.ModelChara = new LazyRow<ModelChara>(gameData, parser.ReadColumn<ushort>(5), language);
			this.CustomizeSheet = new LazyRow<BNpcCustomizeSheet>(gameData, parser.ReadColumn<ushort>(6), language);
			this.NpcEquipment = new LazyRow<NpcEquipment>(gameData, parser.ReadColumn<ushort>(7), language);
		}
		
		// INpcBase

		public string Name { get; set; } = string.Empty;

		public ushort GetModelId()
			=> (ushort)this.ModelChara.Row;
		
		public Customize? GetCustomize()
			=> this.CustomizeSheet.Row != 0 ? this.CustomizeSheet.Value?.Customize : null;

		public Equipment? GetEquipment()
			=> this.NpcEquipment?.Value?.Equipment;

		public WeaponEquip? GetMainHand()
			=> this.NpcEquipment?.Value?.MainHand;

		public WeaponEquip? GetOffHand()
			=> this.NpcEquipment?.Value?.OffHand;
		
		// Customize Sheet
		
		[Sheet("BNpcCustomize", columnHash: 0x18f060d4)]
		private class BNpcCustomizeSheet : ExcelRow {
			public Customize Customize { get; set; }
			
			public override void PopulateData(RowParser parser, GameData gameData, Language language) {
				base.PopulateData(parser, gameData, language);
                
				this.Customize = parser.ReadCustomize(0);
			}
		}
	}
}
