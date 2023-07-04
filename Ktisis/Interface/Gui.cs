using System;
using System.Reflection;
using System.Collections.Generic;

using JetBrains.Annotations;

using Dalamud.Interface.Windowing;

using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Core.Providers;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;

namespace Ktisis.Interface; 

public class Gui : Singleton, IEventClient {
	// Reflection cache

	private readonly static FieldInfo WindowsField = typeof(WindowSystem).GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private List<Window> WindowList => (List<Window>?)WindowsField.GetValue(Windows)!;

	// Windowing

	private readonly WindowSystem Windows = new("Ktisis");

	public readonly GuiOverlay Overlay = new();
	
	// Initialize

	public override void Init() {
		Overlay.Init();
		Services.PluginInterface.UiBuilder.DisableGposeUiHide = true;
	}
	
	// OnReady

	public override void OnReady() {
		Services.PluginInterface.UiBuilder.Draw += Draw;
	}
	
	// Draw

	private void Draw() {
		Overlay.Draw();
		Windows.Draw();
	}

	// On enter GPose

	[UsedImplicitly]
	[Listener<GPoseEvent>]
	private void OnGPoseUpdate(object sender, bool isActive) {
		// TODO: Configuration
		if (isActive)
			GetWindow<Workspace>().Open();
		else
			RemoveWindow<Workspace>();
	}
	
	// Disposal

	public override void Dispose() {
		Services.PluginInterface.UiBuilder.Draw -= Draw;
		Overlay.Dispose();
	}
	
	// Windowing

	internal T GetWindow<T>() where T : GuiWindow {
		foreach (var _window in WindowList)
			if (_window is T result) return result;
		var window = (T)Activator.CreateInstance(typeof(T), new object[] { this })!;
		Windows.AddWindow(window);
		return window;
	}

	internal void RemoveWindow<T>() where T : GuiWindow
		=> WindowList.RemoveAll(w => w is T);

	internal void RemoveWindow<T>(T instance) where T : GuiWindow
		=> WindowList.RemoveAll(w => w == instance);

	// Helpers

	internal static string GenerateId<T>(T node) where T : notnull
		=> $"{node.GetType().Name}#{node.GetHashCode():X}";
}