using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;

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
#if TESTING
	) : base($"{Ktisis.Locale.Translate(localeWindowName)} [TESTING]{windowId}", flags, forceMainWindow) {
#else
	) : base($"{Ktisis.Locale.Translate(localeWindowName)}{windowId}", flags, forceMainWindow) {
#endif
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
	
	// Testing branch stuff
	#if TESTING
	private ImRaii.ColorDisposable? _windowColor;
	public override void PreDraw() {
		this._windowColor = new ImRaii.ColorDisposable();
		
		this.PushColor(ImGuiCol.TitleBg);
		this.PushColor(ImGuiCol.TitleBgCollapsed);
		this.PushColor(ImGuiCol.TitleBgActive);
	}
	public unsafe void PushColor(ImGuiCol col) {
		var colVec = ImGui.GetStyleColorVec4(col);
		Vector4 t = new Vector4(colVec->X, colVec->Y, colVec->Z, colVec->W );
		Vector4* vec = &t;
		var M = MathF.Max(MathF.Max(vec->X, vec->Y), vec->Z);
		var m = MathF.Min(MathF.Min(vec->X, vec->Y), vec->Z);
		var o = M - m;
		float H, S;
		
		if (M == 0) {
			S = 0;
		} else {
			S = o / M;
		}

		if(o == 0) {
			H = 0;
		}else if (M == vec->X) {
			H = 60 * (((vec->Y - vec->Z) / o) % 360);
		}else if (M == vec->Y) {
			H = 60 * (((vec->Z - vec->X) / o) + 2);
		} else {
			H = 60 * (((vec->X - vec->Y) / o) + 4);
		}
		H += .5f;
		ImGui.ColorConvertHSVtoRGB(H, S, M, &vec->X, &vec->Y, &vec->Z);

		this._windowColor!.Push(col, *vec);
	} 
	public override void PostDraw() {
		this._windowColor?.Dispose();
	}
	#endif
}
