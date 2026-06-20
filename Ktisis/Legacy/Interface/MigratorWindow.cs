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
			MinimumSize = new Vector2(550, 50)
		};
		this._dpi = dpi;
		this._migrator = migrator;
		this._cfg = cfg;

		if (this._dpi.ConfigFile.Exists) 
			this._v2Window = new V2MigratorWindow(this._migrator, this._migrator._legacyCfg, this._cfg, Ktisis.Locale);

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
		ImGui.Text($"{Ktisis.Locale.Translate("migrator.mainWindow.main_Desc")}");

		var buttonSize = new Vector2(ImGui.GetContentRegionMax().X * .3f, (ImGui.GetContentRegionMax().X * .3f) * .33f);
		if (this._migrator.v2ConfigExists) {
			if (ImGui.Button(Ktisis.Locale.Translate("migrator.mainWindow.v2.from"), buttonSize)) {
				this._migrator.MigrateConfig();
				this._page++;
				this._migrator.v2ConfigExists = true;
			}
			ImGui.SameLine();
			ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.v2.from_desc"));
		}
		
		if(this._migrator.v3ConfigExists)
		{
			if (ImGui.Button(Ktisis.Locale.Translate("migrator.mainWindow.v3.from"), buttonSize)) {
				this._migrator.v2ConfigExists = false;
				this._page++;
			}
			ImGui.SameLine();
			ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.v3.from_desc"));
		}
		
		var text = this.CanBegin ? Ktisis.Locale.Translate("migrator.mainWindow.skip") : $"{Ktisis.Locale.Translate("migrator.mainWindow.skip")} ({Math.Ceiling((decimal)WaitTime - this._timer.Elapsed.Seconds)}s)";

		using var _ = ImRaii.Disabled(!this.CanBegin && !(ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift)));
		if (ImGui.Button(text, buttonSize)) {
			if(!this._migrator.v2ConfigExists)
				this._migrator.V3Skip();
			this._migrator.Begin();
			this.Close();
		}
		ImGui.SameLine();
		ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.skip_desc"));
		_.Pop();
		
		using var _combo = ImRaii.Combo(Ktisis.Locale.Translate("config.language.selector"), Ktisis.Locale.Data?.MetaData.DisplayName);
		if(_combo.Success)
			foreach (var locales in Ktisis.Locale.AvailableLocales) {
				if(ImGui.Selectable(locales.DisplayName, locales.TechnicalName == Ktisis.Locale.Data?.MetaData.TechnicalName))
				{
					if (locales.TechnicalName != Ktisis.Locale.Data?.MetaData.TechnicalName) {
						this._cfg.File.Locale.LocaleId = locales.TechnicalName;
						Ktisis.Locale.LoadLocale(locales.TechnicalName);
					}
				}
			}
		
	}

	private void DrawV3() {
		ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.v3.main_Desc"));
		ImGui.Spacing();
		if (this._dpi.IsTesting) {
			ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.v3.testing"));
			ImGui.AlignTextToFramePadding();
			ImGui.Text(Ktisis.Locale.Translate("migrator.mainWindow.v3.installer"));
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.ArrowUpRightFromSquare)) {
				this._dpi.OpenPluginInstallerTo(searchText: "Ktisis");
			}
		}

		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.ToggleOpenWindows, true, string.Empty,Ktisis.Locale.Translate("migrator.v3.openWindowToggle") , string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.UseToolbar, false, string.Empty, Ktisis.Locale.Translate("migrator.v3.toolbar"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, Ktisis.Locale.Translate("migrator.v3.keybinds"), string.Empty);
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
				if (this._migrator.v2ConfigExists) 
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

		if ((this._migrator.v2ConfigExists && this._page < 6) || (!this._migrator.v2ConfigExists && this._page == 0)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X  - ImGui.CalcTextSize(Ktisis.Locale.Translate("migrator.next")).X - (ImGui.GetStyle().FramePadding.X  * 2) - .1f);
			if (ImGui.Button(Ktisis.Locale.Translate("migrator.next"))) {
				this._page++;
			}
		} else if ((this._migrator.v2ConfigExists && this._page == 6) || (!this._migrator.v2ConfigExists && this._page == 1)) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - ImGui.CalcTextSize(Ktisis.Locale.Translate("migrator.finish")).X - (ImGui.GetStyle().FramePadding.X * 2) - .1f);
			if (ImGui.Button(Ktisis.Locale.Translate("migrator.finish"))) {
				this._migrator.Begin();
				this.Close();
			}
		}
	}
}
