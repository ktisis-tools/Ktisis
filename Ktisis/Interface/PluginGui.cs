using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Ktisis.Core;
using Ktisis.Core.Services;
using Ktisis.Editing;
using Ktisis.Config;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Overlay;
using Ktisis.Common.Extensions;
using Ktisis.Core.Impl;
using Ktisis.Scene.Objects;

namespace Ktisis.Interface;

[KtisisService]
public class PluginGui : IServiceInit, IDisposable {
	// Service
    
	private readonly GPoseService _gpose;
	private readonly GuiOverlay _overlay;
	private readonly UiBuilder _uiBuilder;
	private readonly IServiceContainer _services;

	public PluginGui(
		GPoseService _gpose,
		GuiOverlay _overlay,
		UiBuilder _uiBuilder,
		IServiceContainer _services
	) {
		this._gpose = _gpose;
		this._overlay = _overlay;
		this._uiBuilder = _uiBuilder;
		this._services = _services;
	}

	public void PreInit() {
        this._gpose.OnGPoseUpdate += OnGPoseUpdate;
	}

	public void Initialize() {
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

	// Events

	private void OnDraw() {
		this._overlay.Draw();
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
	
	// Disposal

	public void Dispose() {
		this._uiBuilder.Draw -= OnDraw;
		this._uiBuilder.OpenConfigUi -= ToggleMainWindow;

		this._gpose.OnGPoseUpdate -= OnGPoseUpdate;

		this.Windows.RemoveAllWindows();
	}
}
