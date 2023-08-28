using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Data;
using Ktisis.Scene;
using Ktisis.Services;
using Ktisis.Interface;
using Ktisis.Interop;

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
			.AddService<CommandService>()
			.AddService<DataService>()
			.AddService<GPoseService>()
			.AddService<InteropService>()
			.AddService<NotifyService>()
			.AddService<PluginGui>()
			.AddService<SceneManager>();

		this.InitTask = Init().ContinueWith(task => {
			this.InitTask = null;
			if (task.Exception == null) return;

			PluginLog.Fatal("Ktisis failed to load due to the following error(s):");
			foreach (var err in task.Exception.InnerExceptions)
				PluginLog.Error(err.ToString());

			this.Services.GetService<NotifyService>()?
				.Error("Ktisis failed to load. Please check your error log for more information.");
		});
	}

	// Initialization

	private async Task Init() {
		await Task.Yield();

		var timer = new Stopwatch();
		timer.Start();

		var cfg = this.Services.GetRequiredService<DataService>();
		await Task.WhenAll(new[] {
			cfg.LoadConfig(),
			InitServices()
		});

		PluginLog.Debug($"Initialization completed in {timer.Elapsed.TotalMilliseconds:00.00}ms");
		timer.Restart();

		this.Services.NotifyReady();

		timer.Stop();
		PluginLog.Debug($"Ready notifier completed in {timer.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private async Task InitServices() {
		await Task.Yield();
		this.Services.GetRequiredService<InteropService>();
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
		this.Services.Dispose();
	}
}
