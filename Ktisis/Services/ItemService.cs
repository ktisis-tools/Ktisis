using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.GameData.Excel;

namespace Ktisis.Services;

[Singleton]
public class ItemService {
	private readonly IDataManager _data;
	
	public ItemService(
		IDataManager data
	) {
		this._data = data;
	}
	
	public ItemSheet? ResolveWeapon(ushort id, ushort secId, ushort variant) {
		var sheet = this._data.GetExcelSheet<ItemSheet>();
		return sheet?.Where(item => item.IsWeapon())
			.FirstOrDefault(item => {
				return item.Model.Id == id && item.Model.Base == secId && item.Model.Variant == variant;
			});
	}
}
