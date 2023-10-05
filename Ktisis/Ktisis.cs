using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Ktisis.Core;
using Ktisis.Core.Impl;
using Ktisis.Config;
using Ktisis.Core.Services;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	// Plugin info

	public string Name => "Ktisis";

	public static string Version = $"v{GetVersion()}";

	public static string VersionName = $"Ktisis (Alpha {Version})";

	// Plugin framework

	private Task? InitTask;

	private readonly ServiceManager Services;

	public static IPluginLog Log { get; private set; } = null!;

	// Ctor called on plugin load

	public Ktisis(
		DalamudPluginInterface api,
		IPluginLog _logger
	) {
		// Service registration

		Log = _logger;
		
		this.Services = new ServiceManager()
			.AddDalamudServices(api);

		this.InitTask = Init().ContinueWith(task => {
			this.InitTask = null;
			if (task.Exception == null) return;

			Ktisis.Log.Fatal("Ktisis failed to load due to the following error(s):");
			foreach (var err in task.Exception.InnerExceptions)
				Ktisis.Log.Error(err.ToString());

			this.Services.GetService<NotifyService>()?
				.Error("Ktisis failed to load. Please check your error log for more information.");

			Dispose();
		});
	}

	// Initialization

	private async Task Init() {
		await Task.Yield();

		var timer = new Stopwatch();
		timer.Start();

		this.Services.AddServices<KtisisServiceAttribute>();

		var cfg = this.Services.GetRequiredService<ConfigService>(); 
		await Task.WhenAll(
			cfg.LoadConfig(),
			Task.Run(this.Services.PreInit)
		);
		
		this.Services.Initialize();

		timer.Stop();
		Ktisis.Log.Debug($"Plugin startup completed in {timer.Elapsed.TotalMilliseconds:0.000}ms");
	}

	// Version info

	public static string GetVersion() {
		return Assembly.GetCallingAssembly().GetName().Version!.ToString(fieldCount: 3);
	}

	// Dispose

	public void Dispose() {
		this.InitTask?.Wait();

		try {
			var cfg = this.Services.GetService<ConfigService>();
			cfg?.SaveConfig();
		} catch (Exception err) {
			Ktisis.Log.Error($"Error occurred during disposal:\n{err}");
		}

		this.Services.Dispose();
	}
}
