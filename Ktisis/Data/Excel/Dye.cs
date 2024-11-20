using System.Numerics;

using Dalamud.Utility;

using ImGuiNET;

using Ktisis.Structs.Extensions;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.Data.Excel {
	[Sheet("Stain")]
	public struct Dye(uint row) : IExcelRow<Dye> {
		public uint RowId => row;
		
		public string Name { get; set; }
		public uint Color { get; set; }
		public byte Shade { get; set; }
		public byte SubOrder { get; set; }
		public bool IsMetallic { get; set; }
		public bool UnknownBool { get; set; }

		public RowRef<Stain> Stain { get; set; }

		public bool IsValid() => Shade != 0;
		public Vector4 ColorVector4
		{
			get {
				var c = ImGui.ColorConvertU32ToFloat4(Color);
				return new Vector4(c.Z, c.Y, c.X, c.W);
			}
		}
		
		public static Dye Create(ExcelPage page, uint offset, uint row) {
			var name = page.ReadColumn<string>(3, offset);
			return new Dye(row) {
				Name = !name.IsNullOrEmpty() ? name : "Undyed", // TODO: translation
				Color = page.ReadColumn<uint>(0, offset),
				Shade = page.ReadColumn<byte>(1, offset),
				SubOrder = page.ReadColumn<byte>(2, offset),
				IsMetallic = page.ReadColumn<bool>(5, offset)
			};
		}
	}
}
