using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using GLib.Popups;
using GLib.Popups.ImFileDialog;

using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Interface.Types;
using Ktisis.Interface.Windows;
using Ktisis.Localization;

namespace Ktisis.Interface; 

[Singleton]
public class GuiManager : IDisposable {
	private readonly DIBuilder _di;
	private readonly IUiBuilder _uiBuilder;
	
	private readonly WindowSystem _ws = new("Ktisis");
	private readonly PopupManager _popup = new();
	
	private readonly List<KtisisWindow> _windows = new();

	public readonly LocaleManager Locale;
	public readonly FileDialogManager FileDialogs;
	
	public GuiManager(
		DIBuilder di,
		IUiBuilder uiBuilder,
		LocaleManager locale,
		FileDialogManager dialogs
	) {
		this._di = di;
		this._uiBuilder = uiBuilder;
		this.Locale = locale;
		this.FileDialogs = dialogs;
	}
	
	// Initialization

	public void Initialize() {
		this._uiBuilder.DisableGposeUiHide = true;
		this._uiBuilder.Draw += this.Draw;
		this._uiBuilder.OpenConfigUi += this.OnOpenConfigUi;
		this.FileDialogs.OnOpenDialog += this.OnOpenDialog;
		this.FileDialogs.Initialize();
	}
	
	// Draw

	private void Draw() {
		this._ws.Draw();
		this._popup.Draw();
		this.FileDialogs.Draw();
	}
	
	// Window management
	
	public T Add<T>(T inst) where T : KtisisWindow {
		this._ws.AddWindow(inst);
		this._windows.Add(inst);
		inst.Closed += this.OnClose;
		Ktisis.Log.Verbose($"Added window: {inst.GetType().Name} ('{inst.WindowName}')");
		return inst;
	}

	public T? Get<T>() where T : KtisisWindow
		=> (T?)this._windows.Find(win => win is T);

	public bool Remove(KtisisWindow inst) {
		var result = this._windows.Remove(inst);
		if (result) {
			this._ws.RemoveWindow(inst);
			inst.Closed -= this.OnClose;
			if (inst is IDisposable iDispose)
				iDispose.Dispose();
			Ktisis.Log.Verbose($"Removed window: {inst.GetType().Name} ('{inst.WindowName}')");
		}
		return result;
	}

	public T Create<T>(params object[] parameters) where T : KtisisWindow {
		var inst = this._di.Create<T>(parameters);
		inst.OnCreate();
		return this.Add(inst);
	}

	public T CreatePopup<T>(params object[] parameters) where T : class, IPopup
		=> this.AddPopupSingleton(this._di.Create<T>(parameters));

	public T GetOrCreate<T>(params object[] parameters) where T : KtisisWindow
		=> this.Get<T>() ?? this.Create<T>(parameters);
	
	// Popups

	public T AddPopup<T>(T popup) where T : class, IPopup {
		this._popup.Add(popup);
		return popup;
	}

	public T AddPopupSingleton<T>(T popup) where T : class, IPopup {
		if (this.GetPopup<T>() is {} inst)
			this._popup.Remove(inst);
		return this.AddPopup(popup);
	}

	public T? GetPopup<T>() where T : class, IPopup
		=> this._popup.Get<T>();
	
	// Events

	private void OnClose(KtisisWindow window) {
		Ktisis.Log.Verbose($"Window {window.GetType().Name} ('{window.WindowName}') closed, removing...");
		this.Remove(window);
	}

	private void OnOpenConfigUi() => this.GetOrCreate<ConfigWindow>().Toggle();

	private void OnOpenDialog(FileDialog dialog) {
		foreach (var open in this._popup.GetAll<FileDialog>()) {
			if (open.Title == dialog.Title)
				open.Close();
		}

		this.AddPopup(dialog);
	}
	
	// Disposal

	private void RemoveAll() {
		foreach (var window in this._windows.ToList())
			this.Remove(window);
		this._windows.Clear();
	}
	
	public void Dispose() {
		this._uiBuilder.Draw -= this.Draw;
		this._uiBuilder.OpenConfigUi -= this.OnOpenConfigUi;
		this.RemoveAll();
	}
}
