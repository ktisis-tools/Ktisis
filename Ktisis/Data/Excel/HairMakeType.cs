using System.Collections.Generic;
using System.Linq;

using Ktisis.Structs.Extensions;

using Lumina.Excel;

namespace Ktisis.Data.Excel {
	[Sheet("HairMakeType")]
	public struct HairMakeType(uint row) : IExcelRow<HairMakeType> {
		public uint RowId => row;
		
		// Properties

		public const uint HairLength = 100;
		public const uint FacepaintLength = 50;

		public uint HairStartIndex { get; set; } // 66
		public uint FacepaintStartIndex { get; set; } // 82

		public List<RowRef<CharaMakeCustomize>> HairStyles { get; set; }
		public List<RowRef<CharaMakeCustomize>> Facepaints { get; set; }

		// Build sheet

		static HairMakeType IExcelRow<HairMakeType>.Create(ExcelPage page, uint offset, uint row) {
			var hairStartIndex = page.ReadColumn<uint>(66, offset);
			var facePaintStartIndex = page.ReadColumn<uint>(82, offset);
			return new HairMakeType(row) {
				HairStartIndex = hairStartIndex,
				FacepaintStartIndex = facePaintStartIndex,
				HairStyles = GetRange(page, hairStartIndex, HairLength).ToList(),
				Facepaints = GetRange(page, facePaintStartIndex, FacepaintLength).ToList()
			};
		}

		private static IEnumerable<RowRef<CharaMakeCustomize>> GetRange(ExcelPage page, uint start, uint length) {
			for (var i = start; i < start + length; i++)
				yield return new RowRef<CharaMakeCustomize>(page.Module, i, page.Language);
		}
	}
}
