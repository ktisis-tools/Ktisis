using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

using Lumina.Excel.GeneratedSheets;

using Dalamud.Logging;

using Ktisis.Data.Npc;
using Ktisis.Data.Excel;
using Ktisis.Structs.Extensions;

namespace Ktisis.Util {
	public static class NpcImportService {
		// Name index

		private static Stream GetNameIndexStream() {
			var assembly = Assembly.GetExecutingAssembly();
			var assemblyName = assembly.GetName().Name!;

			var path = $"{assemblyName}.Data.Library.bnpc-index.json";
			
			var stream = assembly.GetManifestResourceStream(path);
			if (stream == null)
				throw new FileNotFoundException(path);

			return stream;
		}

		private static async Task<Dictionary<string, uint>> GetNameIndex() {
			var stream = GetNameIndexStream();

			using var reader = new StreamReader(stream);
			
			var content = await reader.ReadToEndAsync();
			var dict = JsonConvert.DeserializeObject<Dictionary<string, uint>>(content);

			return dict ?? new Dictionary<string, uint>();
		}
		
		// NPC List
		
		public static async Task<IEnumerable<INpcBase>> GetNpcList() {
			await Task.Yield();

			var timer = new Stopwatch();
			timer.Start();

			var result = Enumerable.Empty<INpcBase>();

			var battleTask = GetBattleNpcs();
			var residentTask = GetResidentNpcs();
			await Task.WhenAll(battleTask, residentTask);

			if (battleTask.Result != null)
				result = result.Concat(battleTask.Result);

			if (residentTask.Result != null)
				result = result.Concat(residentTask.Result);

			var list = result.DistinctBy(npc => (
				npc.Name,
				npc.GetModelId(),
				npc.GetCustomize(),
				npc.GetEquipment()
			));

			timer.Stop();
			Logger.Information($"NPC list retrieved in {timer.Elapsed.TotalMilliseconds:0.00}ms");

			return list;
		}
		
		// BattleNpcs

		private static async Task<IEnumerable<INpcBase>?> GetBattleNpcs() {
			await Task.Yield();
			
			var nameIndexTask = GetNameIndex();
			
			var npcSheet = Services.DataManager.GetExcelSheet<BattleNpc>();
			if (npcSheet == null) return null;

			var namesSheet = Services.DataManager.GetExcelSheet<BNpcName>();
			if (namesSheet == null) return null;

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
		
		// ResidentNpcs

		private static async Task<IEnumerable<INpcBase>?> GetResidentNpcs() {
			await Task.Yield();
			
			var npcSheet = Services.DataManager.GetExcelSheet<ResidentNpc>();
			return npcSheet?.Where(npc => npc.Map != 0);
		}
	}
}
