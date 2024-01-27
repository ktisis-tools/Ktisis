using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Ktisis.Services.Plugin; 

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DalamudServices {
	[PluginService] private IChatGui ChatGui { get; set; } = null!;
	[PluginService] private IClientState ClientState { get; set; } = null!;
	[PluginService] private ICommandManager Cmd { get; set; } = null!;
	[PluginService] private IFramework Framework { get; set; } = null!;
	[PluginService] private IGameGui Gui { get; set; } = null!;
	[PluginService] private IGameInteropProvider Interop { get; set; } = null!;
	[PluginService] private IObjectTable ObjectTable { get; set; } = null!;
	[PluginService] private IKeyState KeyState { get; set; } = null!;
	[PluginService] private IDataManager Data { get; set; } = null!;
	[PluginService] private ITextureProvider Tex { get; set; } = null!;
	[PluginService] private ISigScanner SigScanner { get; set; } = null!;

	public void Add(DalamudPluginInterface dpi, IServiceCollection services) {
		dpi.Inject(this);
		services.AddSingleton(dpi)
			.AddSingleton(dpi.UiBuilder)
			.AddSingleton(this.ClientState)
			.AddSingleton(this.Cmd)
			.AddSingleton(this.Framework)
			.AddSingleton(this.Gui)
			.AddSingleton(this.Interop)
			.AddSingleton(this.ObjectTable)
			.AddSingleton(this.KeyState)
			.AddSingleton(this.Data)
			.AddSingleton(this.Tex)
			.AddSingleton(this.SigScanner)
			.AddSingleton(this.ChatGui);
	}
}
