using Ktisis.Common.Extensions;

using Lumina.Excel;

namespace Ktisis.GameData.Excel;

[Sheet("Glasses", columnHash: 0x2faac2c1)]
public struct Glasses(ExcelPage page, uint offset, uint row) : IExcelRow<Glasses> {
	public ExcelPage ExcelPage => page;
	public uint RowOffset { get; } = offset;
	public uint RowId { get; } = row;

	public string Name { get; set; } = string.Empty;
	public uint Icon { get; set; }

	static Glasses IExcelRow<Glasses>.Create(ExcelPage page, uint offset, uint row) {
		return new Glasses(page, offset, row) {
			Name = page.ReadColumn<string>(13, offset),
			Icon = (uint)page.ReadColumn<int>(2, offset)
		};
	}
}
