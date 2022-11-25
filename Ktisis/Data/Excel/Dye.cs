using System.Numerics;

using ImGuiNET;

using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace Ktisis.Data.Excel {
	[Sheet("Stain")]
	public class Dye : ExcelRow {
		public string Name { get; set; } = "";
		public uint Color { get; set; }
		public byte Shade { get; set; }
		public byte SubOrder { get; set; }
		public bool IsMetallic { get; set; }
		public bool UnknownBool { get; set; }

		public LazyRow<Stain> Stain { get; set; } = null!;

		public bool IsValid() => Shade != 0;
		public Vector4 ColorVector4
		{
			get {
				var c = ImGui.ColorConvertU32ToFloat4(Color);
				return new Vector4(c.Z, c.Y, c.X, c.W);
			}
		}

		public override void PopulateData(RowParser parser, Lumina.GameData gameData, Language language) {
			base.PopulateData(parser, gameData, language);

			Name = parser.ReadColumn<string>(3)!;
			Color = parser.ReadColumn<uint>(0);
			Shade = parser.ReadColumn<byte>(1);
			SubOrder = parser.ReadColumn<byte>(2);
			IsMetallic = parser.ReadColumn<bool>(4);

			if (Name == "")
				Name = "Undyed"; // TODO: translation
		}
	}
}