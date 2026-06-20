using System;

using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

using Ktisis.Events;

namespace Ktisis.Interface.Types; 

public abstract class KtisisWindow : Window {
	public delegate void ClosedDelegate(KtisisWindow window);

	private readonly Event<Action<KtisisWindow>> _closedEvent = new();
	public event ClosedDelegate Closed {
		add => this._closedEvent.Add(value.Invoke);
		remove => this._closedEvent.Remove(value.Invoke);
	}

	internal string _localeWindowName;
	internal string _windowId;
	protected KtisisWindow(
		string localeWindowName,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None,
		string windowId = "",
		bool forceMainWindow = false
	) : base($"{Ktisis.Locale.Translate(localeWindowName)}{windowId}", flags, forceMainWindow) {
		this._localeWindowName = localeWindowName;
		this._windowId = windowId;
		this.RespectCloseHotkey = false;
		Ktisis.Locale.LocaleChanged += this.ChangeWindowLocale;
	}
	
	
	public void Open() => this.IsOpen = true;

	public void Close() {
		try {
			if (!this.IsOpen)
				this.OnClose();
		} finally {
			this.IsOpen = false;
		}
	}

	private void ChangeWindowLocale() {
		this.WindowName = Ktisis.Locale.Translate($"{this._localeWindowName}") + this._windowId;
	}

	public virtual void OnCreate() { }

	public override void OnClose() {
		Ktisis.Locale.LocaleChanged -= this.ChangeWindowLocale;
		this._closedEvent.Invoke(this);
	}
	
}
