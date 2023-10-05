using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Ktisis.Core;

namespace Ktisis.Services; 

internal class DalamudServices {
	private readonly DalamudPluginInterface PluginApi;
	
	[PluginService] private ITextureProvider TextureProvider { get; set; } = null!;
	[PluginService] private ICommandManager CommandManager { get; set; } = null!;
	[PluginService] private IGameInteropProvider Interop { get; set; } = null!;
	[PluginService] private IClientState ClientState { get; set; } = null!;
	[PluginService] private IDataManager DataManager { get; set; } = null!;
	[PluginService] private IObjectTable ObjectTable { get; set; } = null!;
	[PluginService] private ISigScanner SigScanner { get; set; } = null!;
	[PluginService] private IFramework Framework { get; set; } = null!;
	[PluginService] private IKeyState KeyState { get; set; } = null!;
	[PluginService] private IGameGui GameGui { get; set; } = null!;

	internal DalamudServices(DalamudPluginInterface api) {
		this.PluginApi = api;
		api.Inject(this);
	}

	internal void AddServices(ServiceManager mgr) => mgr
		.AddInstance(this.PluginApi)
		.AddInstance(this.PluginApi.UiBuilder)
		.AddInstance(this.CommandManager)
		.AddInstance(this.ClientState)
		.AddInstance(this.Interop)
		.AddInstance(this.DataManager)
		.AddInstance(this.TextureProvider)
		.AddInstance(this.ObjectTable)
		.AddInstance(this.SigScanner)
		.AddInstance(this.Framework)
		.AddInstance(this.KeyState)
		.AddInstance(this.GameGui);
}
