using System;
using System.Reflection;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.ImGuiNotification;

using Microsoft.Extensions.DependencyInjection;

using Ktisis.Core;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Ipc;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	public static IPluginLog Log { get; private set; } = null!;
	public static INotificationManager Notification { get; private set; } = null!;

	private readonly ServiceProvider _services;

	public Ktisis(
		IPluginLog logger,
		INotificationManager notification,
		IDalamudPluginInterface dpi
	) {
		Log = logger;
		Notification = notification;
		
		this._services = new ServiceComposer()
			.AddFromAttributes()
			.AddDalamudServices(dpi)
			.AddSingleton(logger)
			.AddSingleton(notification)
			.BuildProvider();

		this._services.GetRequiredService<PluginContext>()
			.Initialize();
		this._services.GetRequiredService<IpcProvider>().RegisterIpc();
	}

	// Version info

	public static string GetVersion() {
		return Assembly.GetCallingAssembly().GetName().Version!.ToString(fieldCount: 3);
	}

	// Notification defs (todo: load from util file?)
	// cred: vfxeditor notification implementation https://github.com/0ceal0t/Dalamud-VFXEditor/blob/main/VFXEditor/Dalamud.cs#L33
	public static void WarningNotification(string content) => Notification.AddNotification(new() {
		Content = content,
		Title = "[Warning] Ktisis",
		Type = NotificationType.Warning,
	});

	// Dispose

	public void Dispose() {
		try {
			this._services.Dispose();
		} catch (Exception err) {
			Log.Error($"Error occurred during disposal:\n{err}");
		}
	}
}
