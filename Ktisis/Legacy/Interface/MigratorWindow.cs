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
	private readonly ConfigManager _cfg;

	public MigratorWindow(
		IDalamudPluginInterface dpi,
		LegacyMigrator migrator,
		ConfigManager cfg
	) : base(
		"Ktisis v3 Setup",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings
	) {
		this.SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = new Vector2(500, 50)
		};
		this._dpi = dpi;
		this._migrator = migrator;
		if (this._migrator.WasUserOnV2) {
			this._v2Window = new V2MigratorWindow(this._migrator, this._migrator._legacyCfg, cfg);
		}
		this._cfg = cfg;

		this.ShowCloseButton = false;
		this.RespectCloseHotkey = false;
	}
	
	private readonly Stopwatch _timer = new();
	private bool _elapsed;
	private int _page = 0;
	
	private const uint ColorYellow = 0xFF00FFFF;
	
	private const int WaitTime = 15;

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
		if (!this._migrator.WasUserOnV2) {
			ImGui.Spacing();
			ImGui.Text("Since you were using the v3 beta, we suggest you disable receive testing versions.\n");
			ImGui.AlignTextToFramePadding();
			ImGui.Text("You can open the plugin installer with this button");
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
				this._dpi.OpenPluginInstallerTo(searchText: "Ktisis");
			}
		}
	}

	public void DrawV3() {
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.ToggleOpenWindows, true, string.Empty, "Toggle open Windows", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.UseToolbar, false, string.Empty, "Use Ktisis Toolbar", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, "Enable Hotkeys", string.Empty);
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
		var text = this.CanBegin ? "Skip and use defaults" : $"Skip and use defaults ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";

		if (this._page == 0) {
			using (var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)))) {
				ImGui.SameLine();
				if (ImGui.Button(text)) {
					if(!this._migrator.WasUserOnV2)
						this._migrator.v3Skip();
					this._migrator.Begin();
					this.Close();
				}
			}
		}
		
		if ((this._migrator.WasUserOnV2 && this._page < 3) || (!this._migrator.WasUserOnV2 && this._page == 0)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetWindowWidth()  - ImGui.CalcTextSize("Next").X - (ImGui.GetStyle().CellPadding.X  * 2) - ImGui.GetStyle().WindowPadding.X - .1f);
			if (ImGui.Button("Next")) {
				this._page++;
			}
		}

		if ((this._migrator.WasUserOnV2 && this._page == 3) || (!this._migrator.WasUserOnV2 && this._page == 1)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Finish").X - (ImGui.GetStyle().CellPadding.X * 2) - ImGui.GetStyle().WindowPadding.X - .1f);
			text = "Finish";
			if (ImGui.Button(text)) {
				if(!this._migrator.WasUserOnV2)
					this._migrator.v3Skip();
				this._migrator.Begin();
				this.Close();
			}
		}
	}
}
