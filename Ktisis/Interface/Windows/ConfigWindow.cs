using System;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Config;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class ConfigWindow : KtisisWindow {
	private readonly ConfigManager _cfg;
	private readonly ContextManager _context;

	private readonly ActionKeybindEditor _keybinds;
	private readonly GizmoStyleEditor _gizmoStyle;

	private Configuration Config => this._cfg.File;

	public ConfigWindow(
		ConfigManager cfg,
		ContextManager context,
		ActionKeybindEditor keybinds,
		GizmoStyleEditor gizmoStyle
	) : base("Ktisis Settings") {
		this._cfg = cfg;
		this._context = context;
		this._keybinds = keybinds;
		this._gizmoStyle = gizmoStyle;
	}
	
	// Open

	public override void OnOpen() {
		this._keybinds.Setup();
	}
	
	// Draw

	public override void Draw() {
		using var tabs = ImRaii.TabBar("##ConfigTabs");
		if (!tabs.Success) return;
		DrawTab("Categories", this.DrawCategoriesTab);
		DrawTab("Gizmo", this.DrawGizmoTab);
		DrawTab("Workspace", this.DrawWorkspaceTab);
		DrawTab("Input", this.DrawInputTab);
	}
	
	// Tabs

	private static void DrawTab(string name, Action handler) {
		using var tab = ImRaii.TabItem(name);
		if (!tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}
	
	// Categories

	private void DrawCategoriesTab() {
		ImGui.Checkbox("Display NSFW bones", ref this.Config.Categories.ShowNsfwBones);
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text("Requires IVCS or any custom skeleton.");
		}
	}
	
	// Gizmo

	private void DrawGizmoTab() {
		ImGui.Checkbox("Flip axis to face camera", ref this.Config.Gizmo.AllowAxisFlip);
		
		ImGui.Spacing();
		ImGui.Text("Style:");
		ImGui.Spacing();
		this._gizmoStyle.Draw();
	}
	
	// Workspace
	
	private void DrawWorkspaceTab() {
		ImGui.Checkbox("Open on entering GPose", ref this.Config.Editor.OpenOnEnterGPose);
	}
	
	// Input

	private void DrawInputTab() {
		ImGui.Checkbox("Enable keybinds", ref this.Config.Keybinds.Enabled);
		ImGui.Spacing();
		this._keybinds.Draw();
	}
	
	// Close handler

	public override void OnClose() {
		base.OnClose();
		this._cfg.Save();
		this._context.Current?.Scene.Refresh();
	}
}
