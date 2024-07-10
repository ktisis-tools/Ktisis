using System;
using System.Reflection;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Microsoft.Extensions.DependencyInjection;

using Ktisis.Core;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	public static IPluginLog Log { get; private set; } = null!;

	private readonly ServiceProvider _services;

	public Ktisis(
		IPluginLog logger,
		IDalamudPluginInterface dpi
	) {
		Log = logger;
		
		this._services = new ServiceComposer()
			.AddFromAttributes()
			.AddDalamudServices(dpi)
			.AddSingleton(logger)
			.BuildProvider();

		this._services.GetRequiredService<PluginContext>()
			.Initialize();
	}

	// Version info

	public static string GetVersion() {
		return Assembly.GetCallingAssembly().GetName().Version!.ToString(fieldCount: 3);
	}

	// Dispose

	public void Dispose() {
		try {
			this._services.Dispose();
		} catch (Exception err) {
			Log.Error($"Error occurred during disposal:\n{err}");
		}
	}
}
