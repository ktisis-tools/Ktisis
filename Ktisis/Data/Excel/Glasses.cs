using Dalamud.Utility;

using Lumina.Data;
using Lumina.Excel;

namespace Ktisis.Data.Excel {
	[Sheet("Glasses")]
	public class Glasses : ExcelRow {
		public string Name { get; set; } = string.Empty;

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			var name = parser.ReadColumn<string>(13);
			this.Name = !name.IsNullOrEmpty() ? name : "None";
		}
	}
}