using Lumina.Data;
using Lumina.Excel;

namespace Ktisis.GameData.Excel;

[Sheet("Glasses", columnHash: 0x2faac2c1)]
public class Glasses : ExcelRow {
	public string Name { get; set; } = string.Empty;
	public uint Icon { get; set; }
	
	public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
		base.PopulateData(parser, gameData, language);

		this.Icon = (uint)parser.ReadColumn<int>(2);
		this.Name = parser.ReadColumn<string>(13) ?? string.Empty;
	}
}
