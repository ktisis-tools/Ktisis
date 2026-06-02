using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using GLib.Widgets;

using Ktisis.Data.Config;
using Ktisis.Interface.Types;

namespace Ktisis.Legacy.Interface;

public class MigratorWindow : KtisisWindow {
	private readonly IDalamudPluginInterface _dpi;
	private readonly LegacyMigrator _migrator;
	private readonly V2MigratorWindow? _v2Window;

	public MigratorWindow(
		IDalamudPluginInterface dpi,
		LegacyMigrator migrator,
		ConfigManager cfg
	) : base(
		"Ktisis v3 Setup",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings
	) {
		this.SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = new Vector2(400, 50)
		};
		this._dpi = dpi;
		this._migrator = migrator;
		if (this._migrator.WasUserOnV2) {
			this._v2Window = new V2MigratorWindow(this._migrator, this._migrator._legacyCfg, cfg);
		}

		this.ShowCloseButton = false;
		this.RespectCloseHotkey = false;
	}
	
	private readonly Stopwatch _timer = new();
	private bool _elapsed;
	private int _page = 0;
	
	private const uint ColorYellow = 0xFF00FFFF;
	
	private const int WaitTime = 10;

	private bool CanBegin => this._timer.Elapsed.TotalSeconds >= WaitTime || this._elapsed;

	public override void OnOpen() {
		this._timer.Reset();
		this._timer.Start();
		this._elapsed = false;
	}

	public void DrawIntroPage() {
		var v2text = "Ktisis v3 was built to ensure that going forward, we can bring new features to our users, you, more easily.\nThis required rewriting nearly all of the code blah blah blah you can skip this and use the defaults if you want by pressing skip.";
		var v3text = "A lot has changed since we started working on v3, and as we've worked on Ktisis we've changed and improved some features.\nThese settings below have new defaults, you can keep your old settings if you wish to, or you can use the new recommended settings.";
		ImGui.Text($"Thank you for using Ktisis! v3 is now our stable version.\n{(this._migrator.WasUserOnV2? v2text : v3text)}");
	}

	public void DrawV3() {

		
	}
	
	/* TODO: V3 changed settings
	 * AutoSaveConfig.OnDisable = false?
	 * ToggleOpenWindows = true
	 * InputConfig.Enabled = false
	 */
	public override void Draw() {
		if (!this._elapsed && this.CanBegin) {
			this._timer.Stop();
			this._elapsed = true;
		}

	
		switch (this._page) {
			case 0:
				this.DrawIntroPage();
				break;
			case 1:
				if(this._migrator.WasUserOnV2)
					this._v2Window?.DrawEditor();
				else 
					this.DrawV3();
				break;
			case 2:
				this._v2Window?.DrawInput();
				break;
			case 3:
				this._v2Window?.DrawOverlay();
				break;
		}
		ImGui.Spacing();
		this.DrawBottomBar();
	}

	public void DrawBottomBar() {
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		var text = this.CanBegin ? "Begin" : $"Begin ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";
		ImGui.SetCursorPosX( ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X - ImGui.CalcTextSize("Next").X - (ImGui.GetStyle().CellPadding.X * 2) - .1f);
		if (this._migrator.WasUserOnV2 && this._page < 3) {
			if (ImGui.Button("Next")) {
				this._page++;
			}
		}
		
		using (var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)))) {
			if (ImGui.Button(text)) {
				this._migrator.Begin();
				this.Close();
			}
		}
	}
}
