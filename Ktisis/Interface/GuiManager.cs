using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using GLib.Popups;

using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Interface.Types;
using Ktisis.Localization;

namespace Ktisis.Interface; 

[Singleton]
public class GuiManager : IDisposable {
	private readonly DIBuilder _di;
	private readonly UiBuilder _uiBuilder;

    private readonly List<KtisisWindow> Windows = new();
	
	private readonly WindowSystem _ws = new("Ktisis");
	private readonly PopupManager _popup = new();
	
	public GuiManager(
		DIBuilder di,
		UiBuilder uiBuilder,
		LocaleManager locale
	) {
		this._di = di;
		this._uiBuilder = uiBuilder;
	}
	
	// Initialization

	public void Initialize() {
		this._uiBuilder.DisableGposeUiHide = true;
		this._uiBuilder.Draw += this.Draw;
	}
	
	// Draw

	private void Draw() {
		this._ws.Draw();
		this._popup.Draw();
	}
	
	// Window management
	
	public T Add<T>(T inst) where T : KtisisWindow {
		this._ws.AddWindow(inst);
		this.Windows.Add(inst);
		inst.Closed += this.OnClose;
		Ktisis.Log.Verbose($"Added window: {inst.GetType().Name} ('{inst.WindowName}')");
		return inst;
	}

	public T? Get<T>() where T : KtisisWindow
		=> (T?)this.Windows.Find(win => win is T);

	public bool Remove(KtisisWindow inst) {
		var result = this.Windows.Remove(inst);
		if (result) {
			this._ws.RemoveWindow(inst);
			inst.Closed -= this.OnClose;
			if (inst is IDisposable iDispose)
				iDispose.Dispose();
			Ktisis.Log.Verbose($"Removed window: {inst.GetType().Name} ('{inst.WindowName}')");
		}
		return result;
	}
	
	public T Create<T>(params object[] parameters) where T : KtisisWindow
		=> this.Add(this._di.Create<T>(parameters));

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
	
	// Disposal

	private void RemoveAll() {
		foreach (var window in this.Windows.ToList())
			this.Remove(window);
		this.Windows.Clear();
	}
	
	public void Dispose() {
		this._uiBuilder.Draw -= this.Draw;
		this.RemoveAll();
	}
}
