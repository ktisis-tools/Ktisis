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

	protected KtisisWindow(
		string name,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None,
		bool forceMainWindow = false
	) : base(name, flags, forceMainWindow) {
		this.RespectCloseHotkey = false;

		this.SetHelpButton();
	}

	// help link

	public string HelpUrl = "https://sleepybnuuy.github.io/ktisis-docs/";
	private void SetHelpButton() {
		this.TitleBarButtons.Add(new() {
			Icon = FontAwesomeIcon.QuestionCircle,
			IconOffset = new Vector2(2.5f, 1.0f),
			ShowTooltip = () => {
				using var _ = ImRaii.Tooltip();
				ImGui.Text("Open Ktisis Docs...");
			},
			Click = _ => GuiHelpers.OpenBrowser(this.HelpUrl)
		});
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
		this._closedEvent.Invoke(this);
	}
}
