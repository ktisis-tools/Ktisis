using System;
using System.Reflection;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Ktisis.Core;
using Ktisis.Services;
using Ktisis.Scene;
using Ktisis.Scene.Editing;
using Ktisis.Data.Config;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Overlay;
using Ktisis.Common.Extensions;

namespace Ktisis.Interface; 

public class PluginGui : IDisposable {
	// Service

	private readonly ConfigService _cfg;
	private readonly UiBuilder _uiBuilder;
	private readonly GPoseService _gpose;
	private readonly IServiceContainer _services;
	
	public PluginGui(
		ConfigService _cfg,
		UiBuilder _uiBuilder,
		GPoseService _gpose,
		SceneManager _scene,
		IServiceContainer _services
	) {
		this._cfg = _cfg;
		this._services = _services;
		this._uiBuilder = _uiBuilder;
		this._gpose = _gpose;

		this.Overlay = _services.Inject<GuiOverlay>();

		_uiBuilder.Draw += OnDraw;
		_uiBuilder.OpenConfigUi += ToggleMainWindow;
		_uiBuilder.DisableGposeUiHide = true;

		_gpose.OnGPoseUpdate += OnGPoseUpdate;
		_scene.Editor.Selection.OnSelectionChanged += OnSelectionChanged;
	}
	
	// Reflection cache

	private readonly static FieldInfo WindowsField = typeof(WindowSystem)
		.GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
	
	// Overlay

	public readonly GuiOverlay Overlay;
	
	// Window state

	private readonly WindowSystem Windows = new("Ktisis");

	private List<Window> WindowList => (List<Window>)WindowsField.GetValue(this.Windows)!;
	
	// Window access
	
	public T GetWindow<T>() where T : Window {
		if (this.WindowList.Find(w => w is T) is T result)
			return result;

		var window = this._services.Inject<T>();
		this.Windows.AddWindow(window);
		return window;
	}
	
	// Window toggles

	public void ToggleMainWindow() => this.GetWindow<Workspace>().Toggle();
	
	// Events

	private void OnDraw() {
		this.Overlay.Draw();
		this.Windows.Draw();
	}

	private void OnGPoseUpdate(bool active) {
		// TODO: Configuration
		var window = GetWindow<Workspace>();
		if (active)
			window.Open();
		else
			window.Close();
	}

	private void OnSelectionChanged(SelectState _state) {
		if (!this._cfg.Config.Editor_OpenOnSelect) return;

		var window = this.GetWindow<TransformWindow>();
		if (_state.Count > 0)
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
