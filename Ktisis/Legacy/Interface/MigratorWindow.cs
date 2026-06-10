using System;
using System.Diagnostics;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

using GLib.Widgets;

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
			MinimumSize = new Vector2(500, 50)
		};
		this._dpi = dpi;
		this._migrator = migrator;
		this.Locale = locale;
		this._cfg = cfg;

		if (this._migrator.WasUserOnV2 && this._migrator._legacyCfg != null)
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
		ImGui.Text($"{this.Locale.Translate("migrator.mainWindow.main_Desc")}{(this._migrator.WasUserOnV2? this.Locale.Translate("migrator.mainWindow.v2.main_Desc") : this.Locale.Translate("migrator.mainWindow.v3.main_Desc"))}");
		if (!this._migrator.WasUserOnV2) {
			ImGui.Spacing();
			ImGui.Text(this.Locale.Translate("migrator.mainWindow.v3.testing"));
			ImGui.AlignTextToFramePadding();
			ImGui.Text(this.Locale.Translate("migrator.mainWindow.v3.installer"));
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
				this._dpi.OpenPluginInstallerTo(searchText: "Ktisis");
			}
		} else {
			ImGui.Spacing();
			ImGui.AlignTextToFramePadding();
			ImGui.Text(this.Locale.Translate("migrator.mainWindow.v2.wiki"));
			ImGui.SameLine();
			if(Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
				var _ = new ProcessStartInfo{
					FileName = "https://docs.ktisis.tools/migration/",
					UseShellExecute = true
				};
				Process.Start(_);
			}
		}
	}

	private void DrawV3() {
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
			case 4:
				this._v2Window?.DrawAutoSave();
				break;
			case 5:
				this._v2Window?.DrawCamera();
				break;
		}
		ImGui.Spacing();
		this.DrawBottomBar();
	}

	private void DrawBottomBar() {
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		var text = this.CanBegin ? this.Locale.Translate("migrator.mainWindow.skip") : $"{this.Locale.Translate("migrator.mainWindow.skip")} ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";
		if (this._page == 0) {
			using var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)));
			if (ImGui.Button(text)) {
				if(!this._migrator.WasUserOnV2)
					this._migrator.V3Skip();
				this._migrator.Begin();
				this.Close();
			}
		}

		if ((this._migrator.WasUserOnV2 && this._page < 5) || (!this._migrator.WasUserOnV2 && this._page == 0) || (!this._migrator.WasUserOnV2 && !this._cfg.File.Keybinds.Enabled && !this._cfg.File.Editor.ToggleOpenWindows)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetWindowWidth()  - ImGui.CalcTextSize("Next").X - (ImGui.GetStyle().CellPadding.X  * 2) - ImGui.GetStyle().WindowPadding.X - .1f);
			if (ImGui.Button("Next")) {
				this._migrator.MigrateConfig();
				this._page++;
			}
		} else if ((this._migrator.WasUserOnV2 && this._page == 5) || (!this._migrator.WasUserOnV2 && this._page == 1) || (!this._migrator.WasUserOnV2 && this._cfg.File.Keybinds.Enabled && this._cfg.File.Editor.ToggleOpenWindows)) {
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
