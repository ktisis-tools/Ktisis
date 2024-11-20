using Lumina.Excel;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Data.Excel {
	[Sheet("ENpcResident", columnHash: 0xf74fa88c)]
	public struct ResidentNpc(uint row) : IExcelRow<ResidentNpc>, INpcBase {
		// Excel

		public uint RowId => row;
		
		public byte Map { get; set; }
		
		public RowRef<EventNpc> EventNpc { get; set; }
		
		public static ResidentNpc Create(ExcelPage page, uint offset, uint row) {
			var singular = page.ReadColumn<string>(0, offset);
			var article = page.ReadColumn<sbyte>(7, offset);
			return new ResidentNpc(row) {
				Name = singular.FormatName(article) ?? $"E:{row:D7}",
				EventNpc = new RowRef<EventNpc>(page.Module, row, page.Language),
				Map = page.ReadColumn<byte>(9, offset)
			};
		}
		
		// INpcBase

		public string Name { get; set; } = null!;

		public uint HashId { get; set; }

		public ushort GetModelId() => this.EventNpc.Value.GetModelId();
		
		public Customize? GetCustomize() => this.EventNpc.Value.GetCustomize();

		public Equipment? GetEquipment() => this.EventNpc.Value.GetEquipment();

		public WeaponEquip? GetMainHand() => this.EventNpc.Value.GetMainHand();

		public WeaponEquip? GetOffHand() => this.EventNpc.Value.GetOffHand();
	}
}
