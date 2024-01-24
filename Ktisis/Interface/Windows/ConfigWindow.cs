using System;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class ConfigWindow : KtisisWindow {
	private readonly ConfigManager _cfg;
	private readonly ContextManager _context;

	private Configuration Config => this._cfg.Config;

	public ConfigWindow(
		ConfigManager cfg,
		ContextManager context
	) : base("Ktisis Settings") {
		this._cfg = cfg;
		this._context = context;
	}

	public override void Draw() {
		using var _tabs = ImRaii.TabBar("##ConfigTabs");
		DrawTab("Categories", this.DrawCategoriesTab);
		DrawTab("Gizmo", this.DrawGizmoTab);
		DrawTab("Workspace", this.DrawWorkspaceTab);
		DrawTab("Input", this.DrawInputTab);
	}
	
	// Tabs

	private static void DrawTab(string name, Action handler) {
		using var _tab = ImRaii.TabItem(name);
		if (!_tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}
	
	// Categories

	private void DrawCategoriesTab() {
		ImGui.Checkbox("Display NSFW bones", ref this.Config.Categories.ShowNsfwBones);
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _tooltip = ImRaii.Tooltip();
			ImGui.Text("Requires IVCS or any custom skeleton.");
		}
	}
	
	// Gizmo

	private void DrawGizmoTab() {
		ImGui.Checkbox("Flip axis to face camera", ref this.Config.Gizmo.AllowAxisFlip);
	}
	
	// Workspace
	
	private void DrawWorkspaceTab() {
		ImGui.Checkbox("Open on entering GPose", ref this.Config.Editor.OpenOnEnterGPose);
	}
	
	// Input

	private void DrawInputTab() {
		ImGui.Text("Keybinds will become configurable in a later testing release.");
		
		ImGui.Checkbox("Enable keybinds", ref this.Config.Keybinds.Enabled);

		using var _disable = ImRaii.Disabled(!this.Config.Keybinds.Enabled);
		
		ImGui.Text(
			"Currently available keybinds:\n" +
			"	• History - Undo (Ctrl + Z)\n" +
			"	• History - Redo (Ctrl + Shift + Z)\n" +
			"	• Gizmo - Position Mode (Ctrl + T)\n" +
			"	• Gizmo - Rotate Mode (Ctrl + R)\n" +
			"	• Gizmo - Scale Mode (Ctrl + S)\n" +
			"	• Editor - Unselect (Escape)"
		);
	}
	
	// Close handler

	public override void OnClose() {
		base.OnClose();
		this._cfg.Save();
		this._context.Context?.Scene.Refresh();
	}
}
