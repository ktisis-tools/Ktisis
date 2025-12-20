using Dalamud.Utility;

using Ktisis.Structs.Extensions;

using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace Ktisis.Data.Excel {
	[Sheet("Glasses")]
	public struct Glasses(ExcelPage page, uint offset, uint row) : IExcelRow<Glasses> {
		public ExcelPage ExcelPage => page;
		public uint RowOffset => offset;
		public uint RowId => row;
		
		public string Name { get; set; } = string.Empty;

		static Glasses IExcelRow<Glasses>.Create(ExcelPage page, uint offset, uint row) {
			return new Glasses(page, offset, row) {
				Name = row != 0 ? page.ReadColumn<string>(13, offset) : "None"
			};
		}
	}
}