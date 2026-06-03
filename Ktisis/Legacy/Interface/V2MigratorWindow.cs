using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;

using GLib.Widgets;

using Ktisis.Data.Config;

namespace Ktisis.Legacy.Interface;

public class V2MigratorWindow {

	private readonly LegacyMigrator _migrator;
	private ConfigManager _cfg;
	public V2MigratorWindow(
		LegacyMigrator migrator,
		LegacyConfig.Configuration legacyCfg,
		ConfigManager cfg
	) {
		this._migrator = migrator;
		this._cfg = cfg;
	}
	public void DrawEditor() {
		using var _ = ImRaii.PushId("Editor");
		ImGui.Text("Editor settings");
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Categories.ShowNsfwBones, false, "v2 setting: Censor NSFW bones", "Show NSFW Bones", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.IncognitoPlayerNames, false, "v2 setting: Hide Player Names", "Incognito Mode", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.UseToolbar, true, string.Empty, "Use Ktisis Toolbar", "This is recommended for v2 users for a more familiar UI");
	}
	
	public void DrawInput() {
		using var _ = ImRaii.PushId("Input");
		ImGui.Text("Input settings");
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, "Enable Keybinds", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Gizmo.AllowAxisFlip, true, string.Empty, "Flip axis to face camera", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.BlockTargetLeftClick, false, string.Empty, "Disable GPose change target on left click", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Keybinds.BlockTargetRightClick, false, string.Empty, "Disable GPose change target on right click", string.Empty);
	}
	
	public void DrawOverlay() {
		using var _ = ImRaii.PushId("Overlay");
		ImGui.Text("Overlay settings");
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawLines, true, string.Empty, "Draw lines on skeleton", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawLinesGizmo, true, string.Empty, "Draw lines on skeleton while using gizmo", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DrawDotsGizmo, true, string.Empty, "Draw dots while using gizmo", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineThickness, 2.0f, string.Empty, "Line thickness", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineOpacity, 0.95f, string.Empty, "Line opacity",string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.LineOpacityUsing, 0.15f, string.Empty, "Line opacity using",string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Overlay.DotRadius, 7.0f, string.Empty, "Dot radius",string.Empty);
	}

	public void DrawAutoSave() {
		using var _ = ImRaii.PushId("Auto save");
		ImGui.Text("Auto save settings");
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Enabled, true, string.Empty, "Enabled", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Interval, 60 , string.Empty, "Interval", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.Count, 5 , string.Empty, "Number to save", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.ClearOnExit, false , string.Empty, "Clear on Exit", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.AutoSave.OnDisconnect, true , string.Empty, "Autosave on Disconnect", string.Empty);
	}

	public void DrawCamera() {
		using var _ = ImRaii.PushId("Work Camera");
		ImGui.Text("Work Camera settings");
		ImGui.Separator();
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamSens, 0.215f, string.Empty, "Sensitivity", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamMoveSpeed, 0.1f, string.Empty, "Movement speed", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamFastMulti, 2.5f, string.Empty, "Fast Multiplier", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamSlowMulti, 0.25f, string.Empty, "Slow Multiplier", string.Empty);
		DialogHelpers.BuildDialog(ref this._cfg.File.Editor.WorkcamVertMulti, 1f, string.Empty, "Vertical Multiplier", string.Empty);
	}
}
