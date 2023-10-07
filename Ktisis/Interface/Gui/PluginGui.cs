using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Ktisis.Common.Extensions;
using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Interface.Gui.Menus;
using Ktisis.Interface.Gui.Overlay;
using Ktisis.Interface.Gui.Windows;
using Ktisis.Services;

namespace Ktisis.Interface.Gui; 

[DIService]
public class PluginGui : IDisposable {
	// Service
	
	private readonly GPoseService _gpose;
	private readonly GuiOverlay _overlay;
	private readonly UiBuilder _uiBuilder;
	private readonly IServiceContainer _services;
	
	public PluginGui(
		GPoseService _gpose,
		GuiOverlay _overlay,
		UiBuilder _uiBuilder,
		IServiceContainer _services,
		InitEvent _init
	) {
		this._gpose = _gpose;
		this._overlay = _overlay;
		this._uiBuilder = _uiBuilder;
		this._services = _services;
		
		_init.Subscribe(Initialize);
	}

	private void Initialize() {
		this._gpose.OnGPoseUpdate += OnGPoseUpdate;
		
		this._uiBuilder.Draw += OnDraw;
		this._uiBuilder.OpenConfigUi += ToggleMainWindow;
		this._uiBuilder.DisableGposeUiHide = true;

		this.Create<TransformWindow>();
	}
	
	// Reflection cache

	private readonly static FieldInfo WindowsField = typeof(WindowSystem)
		.GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
	
	// Window state

	private readonly WindowSystem Windows = new("Ktisis");

	private List<Window> WindowList => (List<Window>)WindowsField.GetValue(this.Windows)!;
	
	// Window access

	private T Create<T>() where T : Window {
		var window = this._services.Inject<T>();
		this.Windows.AddWindow(window);
		return window;
	}
	
	public T GetWindow<T>() where T : Window {
		if (this.WindowList.Find(w => w is T) is T result)
			return result;
		
		return Create<T>();
	}
	
	// Window toggles

	public void ToggleMainWindow() => this.GetWindow<Workspace>().Toggle();
	
	// Context menu

	private ContextMenu? ContextMenu;

	public ContextMenuFactory BuildContextMenu(string id)
		=> new(id, result => this.ContextMenu = result);
		
	// Events

	private void OnDraw() {
		this._overlay.Draw();
		this.Windows.Draw();

		if (this.ContextMenu is ContextMenu ctx && !ctx.Draw())
			this.ContextMenu = null;
	}

	private void OnGPoseUpdate(bool active) {
		// TODO: Configuration
		var window = GetWindow<Workspace>();
		if (active)
			window.Open();
		else
			window.Close();
	}
	
	// Disposal

	public void Dispose() {
		this._uiBuilder.Draw -= OnDraw;
		this._uiBuilder.OpenConfigUi -= ToggleMainWindow;

		this._gpose.OnGPoseUpdate -= OnGPoseUpdate;
		
		this.Windows.RemoveAllWindows();
	}
}
