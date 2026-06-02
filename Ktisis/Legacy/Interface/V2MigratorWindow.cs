using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

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
		this.BuildDialog(ref this._cfg.File.Categories.ShowNsfwBones, false, "v2 setting: Censor NSFW bones", "Show NSFW Bones");
		this.BuildDialog(ref this._cfg.File.Editor.IncognitoPlayerNames, false, "v2 setting: Hide Player Names", "Incognito Mode");
		this.BuildDialog(ref this._cfg.File.Editor.UseToolbar, true, "This is recommended for v2 users for a more familiar UI", "Use Ktisis Toolbar");
	}
	public void DrawInput() {
		using var _ = ImRaii.PushId("Input");
		ImGui.Text("Input settings");
		ImGui.Separator();
		this.BuildDialog(ref this._cfg.File.Keybinds.Enabled, true, string.Empty, "Enable Keybinds");
		this.BuildDialog(ref this._cfg.File.Gizmo.AllowAxisFlip, true, string.Empty, "Flip axis to face camera");
	}

	public void DrawOverlay() {
		using var _ = ImRaii.PushId("Overlay");
		ImGui.Text("Overlay settings");
		ImGui.Separator();
		this.BuildDialog(ref this._cfg.File.Overlay.DrawLines, true, string.Empty, "Draw lines on skeleton");
		this.BuildDialog(ref this._cfg.File.Overlay.DrawLinesGizmo, true, string.Empty, "Draw lines on skeleton while using gizmo");
		this.BuildDialog(ref this._cfg.File.Overlay.DrawDotsGizmo, true, string.Empty, "Draw dots while using gizmo");
		this.BuildDialog(ref this._cfg.File.Overlay.LineThickness, 2.0f, string.Empty, "Line thickness");
		this.BuildDialog(ref this._cfg.File.Overlay.LineOpacity, 0.95f, string.Empty, "Line opacity");
	}


	private void BuildDialog(ref bool newSet, bool newDefault, string tooltipString, string newSettingName) {
		ImGui.AlignTextToFramePadding();
		ImGui.Text(newSettingName);
		if(tooltipString!= string.Empty)//this._migrator.Locale.Translate(newSettingName)
			this.DrawHint(tooltipString);
		ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - ImGui.GetFrameHeight());
		ImGui.Checkbox($"##{newSettingName}", ref newSet);
		ImGui.Indent();
		ImGui.TextDisabled($"Default: {(newDefault? "On":"Off")}");
		ImGui.Unindent();
	}
	
	private void BuildDialog(ref float newSet, float newDefault, string tooltipString, string newSettingName) {
		ImGui.AlignTextToFramePadding();
		ImGui.Text(newSettingName);
		if(tooltipString != string.Empty)
			this.DrawHint(tooltipString);//this._migrator.Locale.Translate(newSettingName)
		ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X - 80f);
		ImGui.PushItemWidth(80f);
		ImGui.InputFloat($"##{newSettingName}", ref newSet);
		ImGui.PopItemWidth();
		ImGui.Indent();
		ImGui.TextDisabled($"Default: {newDefault}");
		ImGui.Unindent();
	}
	
	private void DrawHint(string tooltipString) {
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(tooltipString);
		}
	}
}
