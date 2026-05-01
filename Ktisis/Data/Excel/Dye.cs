using System.Numerics;

using Dalamud.Utility;
using Dalamud.Bindings.ImGui;

using Ktisis.Structs.Extensions;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.Data.Excel {
	[Sheet("Stain")]
	public struct Dye(ExcelPage page, uint offset, uint row) : IExcelRow<Dye> {
		public ExcelPage ExcelPage => page;
		public uint RowOffset => offset;
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
		
		public static Dye Create(ExcelPage page, uint offset, uint row)
		{
			var name = page.ReadString(offset + 4, offset).ToString();

			return new Dye(page, offset, row) {
				Name = !name.IsNullOrEmpty() ? name : "Undyed", // TODO: translation
				Color = page.ReadUInt32(offset + 8),
				Shade = page.ReadUInt8(offset + 20),
				SubOrder = page.ReadUInt8(offset + 21),
				IsMetallic = page.ReadPackedBool(offset + 22, 0)
			};
		}
	}
}
