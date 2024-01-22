using System;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class ConfigWindow : KtisisWindow {
	private readonly ConfigManager _cfg;

	private Configuration Config => this._cfg.Config;

	public ConfigWindow(
		ConfigManager cfg
	) : base("Ktisis Settings") {
		this._cfg = cfg;
	}

	public override void Draw() {
		using var _tabs = ImRaii.TabBar("##ConfigTabs");
		DrawTab("Workspace", this.DrawWorkspaceTab);
		DrawTab("Input", this.DrawActionsTab);
	}
	
	// Tabs

	private static void DrawTab(string name, Action handler) {
		using var _tab = ImRaii.TabItem(name);
		if (!_tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}

	private void DrawActionsTab() {
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

	private void DrawWorkspaceTab() {
		ImGui.Checkbox("Open on entering GPose", ref this.Config.Editor.OpenOnEnterGPose);
	}
	
	// Close handler

	public override void OnClose() {
		base.OnClose();
		this._cfg.Save();
	}
}
