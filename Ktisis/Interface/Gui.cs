using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using JetBrains.Annotations;

using Dalamud.Interface.Windowing;

using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.Game.Engine;

namespace Ktisis.Interface;

public class Gui : Singleton, IEventClient {
	// Reflection cache

	private readonly static FieldInfo WindowsField = typeof(WindowSystem).GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private List<Window> WindowList => (List<Window>?)WindowsField.GetValue(Windows)!;

	// Windowing

	private readonly WindowSystem Windows = new("Ktisis");
	private readonly GuiOverlay Overlay = new();

	// Initialize

	public override void Init() {
		Overlay.Init();
		Ktisis.PluginApi.UiBuilder.DisableGposeUiHide = true;
	}

	// OnReady

	public override void OnReady() {
		Ktisis.PluginApi.UiBuilder.Draw += Draw;
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
		Ktisis.PluginApi.UiBuilder.Draw -= Draw;
		Overlay.Dispose();
	}

	// Windowing

	internal T GetWindow<T>(params object[] args) where T : GuiWindow {
		foreach (var _window in WindowList)
			if (_window is T result) return result;

		var ctorParams = args.Prepend(this).ToArray();
		var window = (T)Activator.CreateInstance(typeof(T), ctorParams)!;
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
