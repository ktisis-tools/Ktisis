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
	private readonly BoneCategoryEditor _boneCategories;
	private readonly GizmoStyleEditor _gizmoStyle;

	private Configuration Config => this._cfg.File;

	public ConfigWindow(
		ConfigManager cfg,
		ContextManager context,
		ActionKeybindEditor keybinds,
		BoneCategoryEditor boneCategories,
		GizmoStyleEditor gizmoStyle
	) : base("Ktisis Settings") {
		this._cfg = cfg;
		this._context = context;
		this._keybinds = keybinds;
		this._boneCategories = boneCategories;
		this._gizmoStyle = gizmoStyle;
	}
	
	// Open

	public override void OnOpen() {
		this._keybinds.Setup();
		this._boneCategories.Setup();
	}
	
	// Draw

	public override void Draw() {
		using var tabs = ImRaii.TabBar("##ConfigTabs");
		if (!tabs.Success) return;
		DrawTab("Categories", this.DrawCategoriesTab);
		DrawTab("Gizmo", this.DrawGizmoTab);
		DrawTab("Overlay", this.DrawOverlayTab);
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
		ImGui.Checkbox("Display NSFW categories", ref this.Config.Categories.ShowNsfwBones);
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text("Requires IVCS or any custom skeleton.");
		}
		
		ImGui.Spacing();
		ImGui.Text("Categories:");
		ImGui.Spacing();
		this._boneCategories.Draw();
	}
	
	// Gizmo

	private void DrawGizmoTab() {
		ImGui.Checkbox("Flip axis to face camera", ref this.Config.Gizmo.AllowAxisFlip);
		
		ImGui.Spacing();
		ImGui.Text("Style:");
		ImGui.Spacing();
		this._gizmoStyle.Draw();
	}
	
	// Overlay

	private void DrawOverlayTab() {
		ImGui.Checkbox("Draw lines on skeleton", ref this.Config.Overlay.DrawLines);
		ImGui.Checkbox("Draw lines on skeleton while using gizmo", ref this.Config.Overlay.DrawLinesGizmo);
		ImGui.Checkbox("Draw dots while using gizmo", ref this.Config.Overlay.DrawDotsGizmo);
		ImGui.Spacing();
		ImGui.DragFloat("Dot radius", ref this.Config.Overlay.DotRadius, 0.1f);
		ImGui.DragFloat("Line thickness", ref this.Config.Overlay.LineThickness, 0.1f);
		ImGui.Spacing();
		ImGui.SliderFloat("Line opacity", ref this.Config.Overlay.LineOpacity, 0.0f, 1.0f);
		ImGui.SliderFloat("Line opacity while using gizmo", ref this.Config.Overlay.LineOpacityUsing, 0.0f, 1.0f);
	}
	
	// Workspace
	
	private void DrawWorkspaceTab() {
		ImGui.Checkbox("Open on entering GPose", ref this.Config.Editor.OpenOnEnterGPose);
	}
	
	// Input

	private void DrawInputTab() {
		ImGui.Checkbox("Enable keybinds", ref this.Config.Keybinds.Enabled);
		if (!this.Config.Keybinds.Enabled) return;
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
