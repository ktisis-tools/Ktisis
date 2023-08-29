using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using ImGuiNET;

using Ktisis.Data;
using Ktisis.Scene;
using Ktisis.Services;
using Ktisis.Interface.Widgets;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows; 

public class Workspace : Window {
	// Service

	private readonly PluginGui _gui;
	private readonly GPoseService _gpose;
	private readonly SceneManager _sceneMgr;
	
	public Workspace(PluginGui _gui, GPoseService _gpose, SceneManager _sceneMgr, DataService _data) : base("Ktisis") {
		this._gui = _gui;
		this._gpose = _gpose;
		this._sceneMgr = _sceneMgr;
        
		this.SceneTree = new SceneTree(_data.GetConfig(), _sceneMgr);
        
		RespectCloseHotkey = false;
	}
	
	// Components

	private readonly SceneTree SceneTree;
	
	// UI draw

	private readonly static Vector2 MinimumSize = new(280, 300);

	public override void Draw() {
		// Set size constraints
		
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};
		
		// Draw scene

		var scene = this._sceneMgr.Scene;
		ImGui.BeginDisabled(scene is null);
		
		DrawStateFrame(scene);

		ImGui.Spacing();

		var style = ImGui.GetStyle();
		var bottomHeight = UiBuilder.IconFont.FontSize + (style.ItemSpacing.Y + style.ItemInnerSpacing.Y) * 2;
		var treeHeight = ImGui.GetContentRegionAvail().Y - bottomHeight;
		this.SceneTree.Draw(treeHeight);

		ImGui.Spacing();

		DrawTreeButtons();
		
		ImGui.EndDisabled();
	}
	
	private void DrawStateFrame(SceneGraph? scene) {
		var style = ImGui.GetStyle();
		var height = (ImGui.GetFontSize() + style.ItemInnerSpacing.Y) * 2 + style.ItemSpacing.Y;
		
		var result = ImGui.BeginChildFrame(102, new Vector2(-1, height));
		if (!result)
			return;
		
		try {
			if (scene != null)
				DrawStateInfo(scene, height);
			else
				ImGui.Text("Waiting for scene...");
		} finally {
			ImGui.EndChildFrame();
		}
	}
	
	private void DrawStateInfo(SceneGraph scene, float height) {
		var padding = ImGui.GetStyle().FramePadding.X;
		
		// Actor name + selection state
		
		ImGui.BeginGroup();

		ImGui.SetCursorPosX(padding * 2);

		var tar = this._gpose.GetTarget();
		ImGui.Text(tar is not null ? tar.Name.TextValue : "No target found!");
		
		ImGui.SetCursorPosX(padding * 2);
        
		var ct = scene.Select.Count;
		if (ct > 0) {
			ImGui.BeginDisabled();
			ImGui.Text($"{ct} item{(ct == 1 ? "" : "s")} selected.");
			ImGui.EndDisabled();
		} else {
			ImGui.TextDisabled("No items selected.");
		}

		ImGui.EndGroup();
		
		// Overlay toggle

		ImGui.SameLine();

		const float ratio = 3/4f;
		var btnSize = new Vector2(height, height) * ratio;
		ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - padding - btnSize.X);
		ImGui.SetCursorPosY(height * (1 - ratio) / 2);
		
		var overlay = this._gui.Overlay.Visible;
		if (Buttons.DrawIconButton(overlay ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, btnSize))
			this._gui.Overlay.Visible = !overlay;
	}

	private void DrawTreeButtons() {
		Buttons.DrawIconButton(FontAwesomeIcon.Plus);
		ImGui.SameLine();
		Buttons.DrawIconButton(FontAwesomeIcon.Filter);
	}
}