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
	}
	
}
