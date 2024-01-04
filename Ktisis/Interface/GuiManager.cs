using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

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
		this._uiBuilder.Draw += this._ws.Draw;
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
		this._uiBuilder.Draw -= this._ws.Draw;
		this.RemoveAll();
	}
}
