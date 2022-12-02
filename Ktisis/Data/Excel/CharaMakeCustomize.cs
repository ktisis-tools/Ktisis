using Lumina.Data;
using Lumina.Excel;
using Lobby = Lumina.Excel.GeneratedSheets.Lobby;

namespace Ktisis.Data.Excel {
	[Sheet("CharaMakeCustomize")]
	public class CharaMakeCustomize : ExcelRow {
		public string Name { get; set; } = "";

		public byte FeatureId { get; set; }
		public uint Icon { get; set; }
		public ushort Data { get; set; }
		public bool IsPurchasable { get; set; }
		public LazyRow<Lobby> Hint { get; set; } = null!;
		public byte FaceType { get; set; }

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			FeatureId = parser.ReadColumn<byte>(0);
			Icon = parser.ReadColumn<uint>(1);
			Data = parser.ReadColumn<ushort>(2);
			IsPurchasable = parser.ReadColumn<bool>(3);
			Hint = new LazyRow<Lobby>(gameData, parser.ReadColumn<uint>(4), language);
			FaceType = parser.ReadColumn<byte>(6);
		}
	}
}
