using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Dalamud.Plugin.Services;

using Lumina.Excel.GeneratedSheets2;

using Ktisis.Core.Attributes;
using Ktisis.GameData.Excel;
using Ktisis.GameData.Excel.Types;
using Ktisis.Common.Extensions;

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

		var nameIndexTask = GetNameIndex();

		var npcSheet = this._data.GetExcelSheet<BattleNpc>()!;
		var namesSheet = this._data.GetExcelSheet<BNpcName>()!;

		var nameIndexDict = await nameIndexTask;
		return npcSheet.Skip(1).Select(row => {
			string? name = null;
			if (nameIndexDict.TryGetValue(row.RowId.ToString(), out var nameIndex)) {
				var nameRow = namesSheet.GetRow(nameIndex);
				name = nameRow?.Singular?.FormatName(nameRow.Article);
			}
			row.Name = name ?? $"B:{row.RowId:D7}";
			return row;
		});
	}

	private async Task<IEnumerable<INpcBase>> GetResidentNpcs() {
		await Task.Yield();
		return this._data.GetExcelSheet<ResidentNpc>()!.Where(npc => npc.Map != 0);
	}
	
	// Gubal BNPC index
	
	private async static Task<Dictionary<string, uint>> GetNameIndex() {
		using var reader = new StreamReader(GetNameIndexStream());
		var content = await reader.ReadToEndAsync();
		return JsonConvert.DeserializeObject<Dictionary<string, uint>>(content) ?? [];
	}
	
	private static Stream GetNameIndexStream() {
		var assembly = Assembly.GetExecutingAssembly();
		var assemblyName = assembly.GetName().Name!;
		
		var path = $"{assemblyName}.Data.Library.bnpc-index.json";

		var stream = assembly.GetManifestResourceStream(path);
		if (stream == null)
			throw new FileNotFoundException(path);
		return stream;
	}
}
