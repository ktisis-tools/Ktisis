using System;
using System.Reflection;
using System.Collections.Generic;

using JetBrains.Annotations;

using Dalamud.Interface.Windowing;

using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Core.Providers;
using Ktisis.Extensions;
using Ktisis.Interface.Common;
using Ktisis.Interface.Windows;

namespace Ktisis.Interface;

public class Gui : Singleton, IEventClient {
	// Reflection cache

	private readonly static FieldInfo WindowsField = typeof(WindowSystem).GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private List<Window> WindowList => (List<Window>?)WindowsField.GetValue(Windows)!;

	// Windowing

	private readonly WindowSystem Windows = new("Ktisis");

	public T GetWindow<T>() where T : GuiWindow {
		foreach (var _window in WindowList)
			if (_window is T result) return result;
		var window = (T)Activator.CreateInstance(typeof(T), new object[] { this })!;
		Windows.AddWindow(window);
		return window;
	}

	public void RemoveWindow<T>() where T : GuiWindow
		=> WindowList.RemoveAll(w => w is T);

	public void RemoveWindow<T>(T instance) where T : GuiWindow
		=> WindowList.RemoveAll(w => w == instance);

	// Initialize

	public override void Init() {
		Services.PluginInterface.UiBuilder.DisableGposeUiHide = true;
		Services.PluginInterface.UiBuilder.Draw += Windows.Draw;
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

	// Dispose

	public override void Dispose() {
		Services.PluginInterface.UiBuilder.Draw -= Windows.Draw;
	}
}
