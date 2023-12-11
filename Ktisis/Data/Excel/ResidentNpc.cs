using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("ENpcResident", columnHash: 0xf74fa88c)]
	public class ResidentNpc : ENpcResident, INpcBase {
		// Excel
		
		public LazyRow<EventNpc> EventNpc { get; set; }

		public override void PopulateData(RowParser parser, GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			this.Name = this.Singular.FormatName(this.Article) ?? $"E:{this.RowId:D7}";
			this.EventNpc = new LazyRow<EventNpc>(gameData, this.RowId, language);
		}
		
		// INpcBase

		public string Name { get; set; } = null!;

		public uint HashId { get; set; }

		public ushort GetModelId() => this.EventNpc.Value?.GetModelId() ?? 0;
		
		public Customize? GetCustomize() => this.EventNpc.Value?.GetCustomize();

		public Equipment? GetEquipment() => this.EventNpc.Value?.GetEquipment();
	}
}
