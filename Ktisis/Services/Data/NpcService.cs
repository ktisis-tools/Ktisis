using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.GameData.Excel;
using Ktisis.GameData.Excel.Types;
using Ktisis.Common.Extensions;

using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;

namespace Ktisis.Services.Data;

[Singleton]
public class NpcService {
	private readonly IDataManager _data;
	
	public NpcService(
		IDataManager data
	) {
		this._data = data;
	}
	
	// Data fetching
	
	public async Task<IEnumerable<INpcBase>> GetNpcList() {
		await Task.Yield();

		var timer = new Stopwatch();
		timer.Start();

		var battleTask = this.GetBattleNpcs();
		var residentTask = this.GetResidentNpcs();
		await Task.WhenAll(battleTask, residentTask);

		var result = battleTask.Result
			.Concat(residentTask.Result)
			.DistinctBy(npc => (
				npc.Name,
				npc.GetModelId(),
				npc.GetCustomize(),
				npc.GetEquipment()
			));

		timer.Stop();
		
		Ktisis.Log.Debug($"NPC list retrieved in {timer.Elapsed.TotalMilliseconds:00.00}ms");

		return result;
	}

	private async Task<IEnumerable<INpcBase>> GetBattleNpcs() {
		await Task.Yield();

		var nameIndex = CsvLoader.LoadResource<BNpcLink>(CsvLoader.BNpcLinkResourceName, false, out var failed, out var exceptions, this._data.GameData, this._data.GameData.Options.DefaultExcelLanguage);
		
		var npcSheet = this._data.GetExcelSheet<BattleNpc>();
		
		return npcSheet.Skip(1).Select(row => {
			string? name = null;
			if (nameIndex.Any((link => link.BNpcBase.RowId == row.RowId))) {
				var nameRow = nameIndex.First(link => link.BNpcBase.RowId == row.RowId).BNpcName;
				name = nameRow.Value.Singular.ExtractText().FormatName(nameRow.Value.Article);
			}
			row.Name = name ?? $"B:{row.RowId:D7}";
			return row;
		}).Cast<INpcBase>();
	}

	private async Task<IEnumerable<INpcBase>> GetResidentNpcs() {
		await Task.Yield();
		return this._data.GetExcelSheet<ResidentNpc>().Where(npc => npc.Map != 0).Cast<INpcBase>();
	}
}
