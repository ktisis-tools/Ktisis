using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Common.Utility;
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

		this.SetTitleBarButtons();
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

	public virtual void OnCreate() { }

	public override void OnClose() {
		Ktisis.Locale.LocaleChanged -= this.ChangeWindowLocale;
		this._closedEvent.Invoke(this);
	}

	private void ChangeWindowLocale() {
		this.WindowName = Ktisis.Locale.Translate($"{this._localeWindowName}") + this._windowId;
		this.SetTitleBarButtons();
	}

	private void SetTitleBarButtons() {
		// used to append a number of TBBs to the top of each Ktisis window
		this.TitleBarButtons.Clear();

		// docs/wiki link
		this.TitleBarButtons.Add(new TitleBarButton {
			Icon = FontAwesomeIcon.QuestionCircle,
			IconOffset = new Vector2(2.0f, 1.0f),
			ShowTooltip = () => {
				using var _ = ImRaii.Tooltip();
				ImGui.Text(Ktisis.Locale.Translate("titlebar.help"));
			},
			Click = _ => GuiHelpers.OpenBrowser(Ktisis.Locale.Translate("titlebar.helpLinkout"))
		});
	}
}
