using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Interface.Types;
using Ktisis.Localization;

namespace Ktisis.Legacy.Interface;

public class MigratorWindow : KtisisWindow {
	private readonly IDalamudPluginInterface _dpi;
	private readonly LegacyMigrator _migrator;
	private readonly V2MigratorWindow? _v2Window;
	private readonly ConfigManager _cfg;
	private readonly LocaleManager Locale;

	public MigratorWindow(
		IDalamudPluginInterface dpi,
		LegacyMigrator migrator,
		ConfigManager cfg,
		LocaleManager locale
	) : base(
		"Ktisis v3 Setup",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings
	) {
		this.SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = new Vector2(550, 50)
		};
		this._dpi = dpi;
		this._migrator = migrator;
		this.Locale = locale;
		this._cfg = cfg;

		if (this._dpi.ConfigFile.Exists) 
			this._v2Window = new V2MigratorWindow(this._migrator, this._migrator._legacyCfg, this._cfg, this.Locale);

		this.ShowCloseButton = false;
		this.RespectCloseHotkey = false;
	}
	
	private readonly Stopwatch _timer = new();
	private bool _elapsed;
	private int _page;
	
	private const int WaitTime = 15;

	private bool CanBegin => this._timer.Elapsed.TotalSeconds >= WaitTime || this._elapsed;

	public override void OnOpen() {
		this._timer.Reset();
		this._timer.Start();
		this._elapsed = false;
	}

	private void DrawIntroPage() {
		ImGui.Text($"{this.Locale.Translate("migrator.mainWindow.main_Desc")}");

		var buttonSize = new Vector2(ImGui.GetContentRegionMax().X * .3f, (ImGui.GetContentRegionMax().X * .3f) * .33f);
		if (this._dpi.ConfigFile.Exists) {
			if (ImGui.Button("I'm coming from v0.2", buttonSize)) {
				this._migrator.MigrateConfig();
				this._page++;
				this._migrator.WasUserOnV2 = true;
			}
			ImGui.SameLine();
			ImGui.Text("Start Mirating based on your Ktisis Settings from v0.2.\nThis will overwrite your existing v0.3 settings.");
		}
		
		if(File.Exists(this._dpi.ConfigDirectory + "\\KtisisV3.json"))
		{
			if (ImGui.Button("I'm coming from v0.3", buttonSize)) {
				this._migrator.WasUserOnV2 = false;
				this._page++;
			}
			ImGui.SameLine();
			ImGui.Text("Start Mirating based on your Ktisis Settings from v0.3.\nThis will ignore any legacy settings from v0.2.");
		}
		
		var text = this.CanBegin ? this.Locale.Translate("migrator.mainWindow.skip") : $"{this.Locale.Translate("migrator.mainWindow.skip")} ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";

		using var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)));
		if (ImGui.Button(text, buttonSize)) {
			if(!this._migrator.WasUserOnV2)
				this._migrator.V3Skip();
			this._migrator.Begin();
			this.Close();
		}
		ImGui.SameLine();
		ImGui.Text("Skip migrating and start v0.3 with default settings.");
		
	}

	private void DrawV3() {
		ImGui.Text(this.Locale.Translate("migrator.mainWindow.v3.main_Desc"));
		ImGui.Spacing();
		if (this._dpi.IsTesting) {
			ImGui.Text(this.Locale.Translate("migrator.mainWindow.v3.testing"));
			ImGui.AlignTextToFramePadding();
			ImGui.Text(this.Locale.Translate("migrator.mainWindow.v3.installer"));
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
				this._dpi.OpenPluginInstallerTo(searchText: "Ktisis");
			}
		}

		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.ToggleOpenWindows, true, string.Empty,this.Locale.Translate("migrator.v3.openWindowToggle") , string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.UseToolbar, false, string.Empty, this.Locale.Translate("migrator.v3.toolbar"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, this.Locale.Translate("migrator.v3.keybinds"), string.Empty);
	}

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
				if (this._migrator.WasUserOnV2) 
					this._v2Window?.DrawIntro();
				else
					this.DrawV3();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
			case 2:
				this._v2Window?.DrawEditor();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
			case 3:
				this._v2Window?.DrawOverlay();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
			case 4:
				this._v2Window?.DrawAutoSave();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
			case 5:
				this._v2Window?.DrawCamera();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
			case 6:
				this._v2Window?.DrawInput();
				ImGui.Spacing();
				this.DrawBottomBar();
				break;
		}

	}

	private void DrawBottomBar() {
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		var text = string.Empty;

		if ((this._migrator.WasUserOnV2 && this._page < 6) || (!this._migrator.WasUserOnV2 && this._page == 0)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetWindowWidth()  - ImGui.CalcTextSize("Next").X - (ImGui.GetStyle().CellPadding.X  * 2) - ImGui.GetStyle().WindowPadding.X - .1f);
			if (ImGui.Button("Next")) {
				this._migrator.MigrateConfig();
				this._page++;
			}
		} else if ((this._migrator.WasUserOnV2 && this._page == 6) || (!this._migrator.WasUserOnV2 && this._page == 1)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Finish").X - (ImGui.GetStyle().CellPadding.X * 2) - ImGui.GetStyle().WindowPadding.X - .1f);
			text = "Finish";
			if (ImGui.Button(text)) {
				this._migrator.Begin();
				this.Close();
			}
		}
	}
}
