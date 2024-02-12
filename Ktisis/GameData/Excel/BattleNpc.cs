using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

using Ktisis.Common.Extensions;
using Ktisis.GameData.Excel.Types;
using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel;

[Sheet("BNpcBase", columnHash: 0xe136dda3)]
public class BattleNpc : ExcelRow, INpcBase {
	public float Scale { get; set; }
	
	private LazyRow<ModelChara> ModelChara { get; set; } = null!;
	private LazyRow<BNpcCustomize> Customize { get; set; } = null!;
	private LazyRow<NpcEquipment> Equipment { get; set; } = null!;
	
	public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
		base.PopulateData(parser, gameData, language);
		this.Scale = parser.ReadColumn<float>(4);
		this.ModelChara = new LazyRow<ModelChara>(gameData, parser.ReadColumn<ushort>(5), language);
		this.Customize = new LazyRow<BNpcCustomize>(gameData, parser.ReadColumn<ushort>(6), language);
		this.Equipment = new LazyRow<NpcEquipment>(gameData, parser.ReadColumn<ushort>(7), language);
	}
	
	// INpcBase

	public string Name { get; set; } = string.Empty;

	public ushort GetModelId() => (ushort)this.ModelChara.Row;

	public CustomizeContainer? GetCustomize() => this.Customize.Row != 0 ? this.Customize.Value?.Customize : null;
	public EquipmentContainer? GetEquipment() => this.Equipment.Value?.Equipment;

	public WeaponModelId? GetMainHand() => this.Equipment.Value?.MainHand;
	public WeaponModelId? GetOffHand() => this.Equipment.Value?.OffHand;
	
	// BNpcCustomize
	
	[Sheet("BNpcCustomize", columnHash: 0x18f060d4)]
	private class BNpcCustomize : ExcelRow {
		public CustomizeContainer Customize { get; private set; }

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);
			this.Customize = parser.ReadCustomize(0);
		}
	}
}
