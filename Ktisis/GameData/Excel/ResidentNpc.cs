using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel;

using Ktisis.Common.Extensions;
using Ktisis.GameData.Excel.Types;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("ENpcResident", columnHash: 0xf74fa88c)]
public struct ResidentNpc(ExcelPage page, uint offset, uint row) : IExcelRow<ResidentNpc>, INpcBase {
	public ExcelPage ExcelPage => page;
	public uint RowOffset { get; } = offset;
	public uint RowId { get; } = row;

	public byte Map { get; init; }
	private RowRef<EventNpc> EventNpc { get; init; }

	static ResidentNpc IExcelRow<ResidentNpc>.Create(ExcelPage page, uint offset, uint row) {
		var singular = page.ReadColumn<string>(0, offset);
		var article = page.ReadColumn<sbyte>(7, offset);
		return new ResidentNpc(page, offset, row) {
			Name = singular.FormatName(article) ?? $"E:{row:D7}",
			Map = page.ReadColumn<byte>(9, offset),
			EventNpc = new RowRef<EventNpc>(page.Module, row, page.Language)
		};
	}
	
	// INpcBase
	
	public string Name { get; set; } = string.Empty;
	
	public uint HashId { get; set; }

	public ushort GetModelId() => this.EventNpc.IsValid ? this.EventNpc.Value.GetModelId() : ushort.MaxValue;

	public CustomizeContainer? GetCustomize() => this.EventNpc.IsValid ? this.EventNpc.Value.GetCustomize() : null;
	public EquipmentContainer? GetEquipment() => this.EventNpc.IsValid ? this.EventNpc.Value.GetEquipment() : null;

	public WeaponModelId? GetMainHand() => this.EventNpc.IsValid ? this.EventNpc.Value.GetMainHand() : null;
	public WeaponModelId? GetOffHand() => this.EventNpc.IsValid ? this.EventNpc.Value.GetOffHand() : null;
}
