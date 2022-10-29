using Lumina.Data;
using Lumina.Excel;

namespace Ktisis.GameData.Excel {
	[Sheet("HairMakeType")]
	public class HairMakeType : ExcelRow {
		// Properties

		public uint HairStartIndex { get; set; } // 66
		public uint FacepaintStartIndex { get; set; } // 82

		// Build sheet

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			HairStartIndex = parser.ReadColumn<uint>(66);
			FacepaintStartIndex = parser.ReadColumn<uint>(82);
		}
	}
}