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

	private string _localeString;
	private string _nameAppend;
	protected KtisisWindow(
		string localeString,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None,
		string nameAppend = "",
		bool forceMainWindow = false
	) : base($"{Ktisis.Locale.Translate(localeString)}{nameAppend}", flags, forceMainWindow) {
		this._nameAppend = nameAppend;
		this._localeString = localeString;
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
		this.WindowName = Ktisis.Locale.Translate($"{this._localeString}") + this._nameAppend;
	}

	public virtual void OnCreate() { }

	public override void OnClose() {
		Ktisis.Locale.LocaleChanged -= this.ChangeWindowLocale;
		this._closedEvent.Invoke(this);
	}
	
}
