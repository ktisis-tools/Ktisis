using System.Threading.Tasks;

using Dalamud.Logging;
using Dalamud.Plugin;

using Ktisis.Data.Config;

namespace Ktisis.Data; 

public class DataService {
	// Service

	private readonly DalamudPluginInterface _api;

	private readonly SchemaReader Schema;

	public DataService(DalamudPluginInterface _api) {
		this._api = _api;
		
		this.Schema = new SchemaReader();
	}
	
	// Configuration

	private ConfigFile? Config;

	public ConfigFile GetConfig() {
		this.Config ??= new ConfigFile();
		return this.Config;
	}

	public async Task LoadConfig() {
		await Task.Yield();
		PluginLog.Verbose("Loading plugin configuration...");
		this.Config = await ConfigFile.Load(this.Schema);
	}
}
