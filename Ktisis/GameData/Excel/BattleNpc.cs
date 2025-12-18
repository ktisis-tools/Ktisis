using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Excel;
using Lumina.Excel.Sheets;

using Ktisis.Common.Extensions;
using Ktisis.GameData.Excel.Types;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("BNpcBase", columnHash: 0xD5D82616)]
public struct BattleNpc(ExcelPage page, uint offset, uint row) : IExcelRow<BattleNpc>, INpcBase {
	public ExcelPage ExcelPage => page;
	public uint RowOffset { get; } = offset;
	public uint RowId { get; } = row;

	public float Scale { get; init; }
	
	private RowRef<ModelChara> ModelChara { get; init; }
	private RowRef<BNpcCustomize> Customize { get; init; }
	private RowRef<NpcEquipment> Equipment { get; init; }

	static BattleNpc IExcelRow<BattleNpc>.Create(ExcelPage page, uint offset, uint row) {
		return new BattleNpc(page, offset, row) {
			Scale = page.ReadColumn<float>(4, offset),
			ModelChara = page.ReadRowRef<ModelChara>(5, offset),
			Customize = page.ReadRowRef<BNpcCustomize>(6, offset),
			Equipment = page.ReadRowRef<NpcEquipment>(7, offset)
		};
	}
	
	// INpcBase

	public string Name { get; set; } = string.Empty;

	public ushort GetModelId() => (ushort)this.ModelChara.RowId;

	public CustomizeContainer? GetCustomize() => this.Customize is { IsValid: true, RowId: not 0 } ? this.Customize.Value.Customize : null;
	public EquipmentContainer? GetEquipment() => this.Equipment.IsValid ? this.Equipment.Value.Equipment : null;

	public WeaponModelId? GetMainHand() => this.Equipment.IsValid ? this.Equipment.Value.MainHand : null;
	public WeaponModelId? GetOffHand() => this.Equipment.IsValid ? this.Equipment.Value.OffHand : null;
	
	// BNpcCustomize
	
	[Sheet("BNpcCustomize", columnHash: 0x18f060d4)]
	private struct BNpcCustomize(ExcelPage page, uint offset, uint row) : IExcelRow<BNpcCustomize> {
		public ExcelPage ExcelPage => page;
		public uint RowOffset { get; } = offset;
		public uint RowId { get; } = row;

		public CustomizeContainer Customize { get; private init; }

		static BNpcCustomize IExcelRow<BNpcCustomize>.Create(ExcelPage page, uint offset, uint row) {
			return new BNpcCustomize(page, offset, row) {
				Customize = page.ReadCustomize(0, offset)
			};
		}
	}
}
