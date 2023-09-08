using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Data;
using Ktisis.Data.Config;
using Ktisis.Scene;
using Ktisis.Services;
using Ktisis.Interface;
using Ktisis.Interop;
using Ktisis.Posing;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	// Plugin info

	public string Name => "Ktisis";

	public static string Version = $"v{GetVersion()}";

	public static string VersionName = $"Ktisis (Alpha {Version})";

	// Plugin framework

	private Task? InitTask;

	private readonly ServiceManager Services;

	// Ctor called on plugin load

	public Ktisis(DalamudPluginInterface api) {
		// Service registration

		this.Services = new ServiceManager()
			.AddDalamudServices(api)
			.AddService<ConfigService>()
			.AddService<CameraService>()
			.AddService<CommandService>()
			.AddService<DataService>()
			.AddService<GPoseService>()
			.AddService<InteropService>()
			.AddService<NotifyService>()
			.AddService<PluginGui>()
			.AddService<PosingService>()
			.AddService<SceneManager>();

		this.InitTask = Init().ContinueWith(task => {
			this.InitTask = null;
			if (task.Exception == null) return;

			PluginLog.Fatal("Ktisis failed to load due to the following error(s):");
			foreach (var err in task.Exception.InnerExceptions)
				PluginLog.Error(err.ToString());

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

		var cfg = this.Services.GetRequiredService<ConfigService>();
		await Task.WhenAll(new[] {
			LoadConfig(cfg),
			InitServices()
		});

		PluginLog.Debug($"Initialization completed in {timer.Elapsed.TotalMilliseconds:0.000}ms");
		timer.Restart();

		this.Services.NotifyReady();

		timer.Stop();
		PluginLog.Debug($"Ready notifier completed in {timer.Elapsed.TotalMilliseconds:0.000}ms");
	}

	private async Task LoadConfig(ConfigService cfg) {
		await Task.Yield();

		var timer = new Stopwatch();
		timer.Start();

		await cfg.LoadConfig();

		timer.Stop();
		PluginLog.Debug($"Loaded configuration in {timer.Elapsed.TotalMilliseconds:0.000}ms");
	}

	private async Task InitServices() {
		await Task.Yield();
		this.Services.GetRequiredService<InteropService>();
		this.Services.GetRequiredService<PosingService>();
		this.Services.GetRequiredService<SceneManager>();
		this.Services.GetRequiredService<CommandService>();
		this.Services.GetRequiredService<PluginGui>();
	}

	// Version info

	public static string GetVersion() {
		return Assembly.GetCallingAssembly().GetName().Version!.ToString(fieldCount: 3);
	}

	// Dispose

	public void Dispose() {
		this.InitTask?.Wait();

		try {
			this.Services.GetService<ConfigService>()?.SaveConfig();
		} catch (Exception err) {
			PluginLog.Error($"Error occurred during disposal:\n{err}");
		}

		this.Services.Dispose();
	}
}
