using System.Collections.Generic;

using Lumina.Data;
using Lumina.Excel;

namespace Ktisis.Data.Excel {
	[Sheet("HairMakeType")]
	public class HairMakeType : ExcelRow {
		// Properties

		public const uint HairLength = 100;
		public const uint FacepaintLength = 50;

		public uint HairStartIndex { get; set; } // 66
		public uint FacepaintStartIndex { get; set; } // 82

		public List<LazyRow<CharaMakeCustomize>> HairStyles { get; set; } = new();
		public List<LazyRow<CharaMakeCustomize>> Facepaints { get; set; } = new();

		// Build sheet

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			HairStartIndex = parser.ReadColumn<uint>(66);
			FacepaintStartIndex = parser.ReadColumn<uint>(82);

			for (var i = HairStartIndex; i < HairStartIndex + HairLength; i++)
				HairStyles.Add(new LazyRow<CharaMakeCustomize>(gameData, i, language));

			for (var i = FacepaintStartIndex; i < FacepaintStartIndex + FacepaintLength; i++)
				Facepaints.Add(new LazyRow<CharaMakeCustomize>(gameData, i, language));
		}
	}
}