using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;

using GLib.Widgets;

using Ktisis.Data.Config;
using Ktisis.Localization;

namespace Ktisis.Legacy.Interface;

public class V2MigratorWindow {

	private readonly LegacyMigrator _migrator;
	private ConfigManager _cfg;
	private readonly LocaleManager Locale;
	public V2MigratorWindow(
		LegacyMigrator migrator,
		LegacyConfig.Configuration legacyCfg,
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._migrator = migrator;
		this._cfg = cfg;
		this.Locale = locale;
	}
	public void DrawEditor() {
		using var _ = ImRaii.PushId("Editor");
		ImGui.Text(this.Locale.Translate("migrator.v2.editor.header"));
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Categories.ShowNsfwBones, false, this.Locale.Translate("migrator.v2.editor.nsfwTooltip"), this.Locale.Translate("migrator.v2.editor.nsfw"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.IncognitoPlayerNames, false, this.Locale.Translate("migrator.v2.editor.incognitoTooltip"), this.Locale.Translate("migrator.v2.editor.incognito"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.UseToolbar, true, string.Empty, this.Locale.Translate("migrator.v2.editor.toolbar"), this.Locale.Translate("migrator.v2.editor.toolbarSub"));
	}

	public void DrawInput() {
		using var _ = ImRaii.PushId("Input");
		ImGui.Text(this.Locale.Translate("migrator.v2.input.header"));
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, this.Locale.Translate("migrator.v2.input.keybinds"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Gizmo.AllowAxisFlip, true, string.Empty, this.Locale.Translate("migrator.v2.input.allowAxisFlip"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.BlockTargetLeftClick, false, string.Empty, this.Locale.Translate("migrator.v2.input.leftClick"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.BlockTargetRightClick, false, string.Empty, this.Locale.Translate("migrator.v2.input.rightClick"), string.Empty);
	}

	public void DrawOverlay() {
		using var _ = ImRaii.PushId("Overlay");
		ImGui.Text(this.Locale.Translate("migrator.v2.overlay.header"));
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawLines, true, string.Empty, this.Locale.Translate("migrator.v2.overlay.drawLines"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawLinesGizmo, true, string.Empty, this.Locale.Translate("migrator.v2.overlay.drawLinesGizmo"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawDotsGizmo, true, string.Empty, this.Locale.Translate("migrator.v2.overlay.drawDotsGizmo"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineThickness, 2.0f, string.Empty, this.Locale.Translate("migrator.v2.overlay.lineThickness"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineOpacity, 0.95f, string.Empty, this.Locale.Translate("migrator.v2.overlay.lineOpacity"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineOpacityUsing, 0.15f, string.Empty, this.Locale.Translate("migrator.v2.overlay.lineOpacityUsing"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DotRadius, 7.0f, string.Empty, this.Locale.Translate("migrator.v2.overlay.dotRadius"), string.Empty);
	}

	public void DrawAutoSave() {
		using var _ = ImRaii.PushId("Auto save");
		ImGui.Text(this.Locale.Translate("migrator.v2.autosave.header"));
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Enabled, true, string.Empty, this.Locale.Translate("migrator.v2.autosave.enabled"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Interval, 60, string.Empty, this.Locale.Translate("migrator.v2.autosave.interval"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Count, 5, string.Empty, this.Locale.Translate("migrator.v2.autosave.numToSave"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.ClearOnExit, false, string.Empty, this.Locale.Translate("migrator.v2.autosave.clearOnExit"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.OnDisconnect, true, string.Empty, this.Locale.Translate("migrator.v2.autosave.autoOnDC"), string.Empty);
	}

	public void DrawCamera() {
		using var _ = ImRaii.PushId("Work Camera");
		ImGui.Text(this.Locale.Translate("migrator.v2.camera.header"));
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamSens, 0.215f, string.Empty, this.Locale.Translate("migrator.v2.camera.sens"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamMoveSpeed, 0.1f, string.Empty, this.Locale.Translate("migrator.v2.camera.movSpeed"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamFastMulti, 2.5f, string.Empty, this.Locale.Translate("migrator.v2.camera.fastMulti"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamSlowMulti, 0.25f, string.Empty, this.Locale.Translate("migrator.v2.camera.slowMulti"), string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamVertMulti, 1f, string.Empty, this.Locale.Translate("migrator.v2.camera.vertMulti"), string.Empty);
	}
}
