using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Scene;
using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Interface.Widgets;
using Ktisis.Interface.Components;
using Ktisis.Data.Config;
using Ktisis.Scene.Editing;

namespace Ktisis.Interface.Windows; 

public class Workspace : Window {
	// Constructor

    private readonly PluginGui _gui;
	private readonly ConfigService _cfg;
	private readonly GPoseService _gpose;
	private readonly PosingService _posing;
	private readonly SceneManager _sceneMgr;

	private ConfigFile Config => this._cfg.Config;
	
	public Workspace(PluginGui _gui, ConfigService _cfg, GPoseService _gpose, PosingService _posing, SceneManager _sceneMgr) : base("Ktisis") {
		this._gui = _gui;
		this._cfg = _cfg;
		this._gpose = _gpose;
		this._posing = _posing;
		this._sceneMgr = _sceneMgr;
		
		this.SceneTree = new SceneTree(_cfg, _sceneMgr);
		
		RespectCloseHotkey = false;
	}
	
	// Components

	private readonly SceneTree SceneTree;
	
	// Constants
	
	private readonly static Vector2 MinimumSize = new(280, 300);

	private readonly static EditMode[] ModeValues = Enum.GetValues<EditMode>().Skip(1).ToArray();
	
	// UI draw

	public override void Draw() {
		// Set size constraints
		
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};

		// Scene access
        
		var scene = this._sceneMgr.Scene;
		ImGui.BeginDisabled(scene is null);
		
		// Draw edit state

		DrawPoseState();
		ImGui.Spacing();
		
		// Draw context buttons
        
		DrawContextButtons();
		ImGui.Spacing();
		
        // Draw scene
        
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
	
	// Pose toggle

	private void DrawPoseState() {
		// Pose toggle
		
		var isPosing = this._posing.IsActive;
		
		ImGui.BeginGroup();

		var color = isPosing ? 0xFF3AD86A : 0xFF504EC4;
		ImGui.PushStyleColor(ImGuiCol.Text, color);
		ImGui.PushStyleColor(ImGuiCol.Button, isPosing ? 0xFF00FF00 : 0xFF7070C0);
		if (Buttons.ToggleButton("KtisisPoseToggle", ref isPosing, color))
			this._posing.Toggle();
		
		var label = isPosing ? "Posing: On" : "Posing: Off";
		ImGui.SameLine();
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetFrameHeight() / 2 - ImGui.CalcTextSize(label).Y / 2);
		ImGui.Text(label);
		
		if (ImGui.IsItemHovered()) {
			ImGui.BeginTooltip();
			ImGui.Text(isPosing ? "Bone manipulation is currently enabled." : "Bone manipulation is currently disabled.");
			ImGui.EndTooltip();
		}
		
		ImGui.PopStyleColor(2);
		
		ImGui.EndGroup();
	}
	
	// Context buttons

	private void DrawContextButtons() {
        if (DrawButton(FontAwesomeIcon.ArrowsAlt, "Transform Editor"))
			this._gui.GetWindow<TransformWindow>().Toggle();
		
		DrawButton(FontAwesomeIcon.Camera, "Camera Editor");
		DrawButton(FontAwesomeIcon.Sun, "Environment Editor");
		DrawButton(FontAwesomeIcon.EllipsisH, "Other...");
        
		var avail = ImGui.GetContentRegionAvail().X;
		if (avail < 100) ImGui.Dummy(Vector2.Zero);

		DrawModeSelect();
	}

	private bool DrawButton(FontAwesomeIcon icon, string hint) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var height = ImGui.GetFrameHeight();
		var width = Math.Max(height, Icons.CalcIconSize(icon).X);
		var activate = Buttons.DrawIconButtonHint(icon, hint, new Vector2(width, height));
		ImGui.SameLine(0, spacing);
		return activate;
	}
	
	// Mode selector

	private void DrawModeSelect() {
		var style = ImGui.GetStyle();
		
		ImGui.BeginGroup();

		var avail = ImGui.GetContentRegionAvail().X;
		var cursor = ImGui.GetCursorPosX();
		
		ImGui.SetNextItemWidth(avail);
		if (ImGui.BeginCombo("##WsEditMode", "")) {
			foreach (var value in ModeValues) {
				if (ImGui.Selectable($"##{value}"))
					this.Config.Editor_Mode = value;
				ImGui.SameLine();
				DrawModeLabel(value);
			}
			ImGui.EndCombo();
		}

		if (ImGui.IsItemHovered()) {
			ImGui.BeginTooltip();
			ImGui.Text("Select editing mode");
			ImGui.EndTooltip();
		}
		
		ImGui.SameLine();
		ImGui.SetCursorPosX(cursor + style.ItemSpacing.X);
		DrawModeLabel(this.Config.Editor_Mode);
		
		ImGui.EndGroup();
	}

	private void DrawModeLabel(EditMode mode) {
		var icon = GetModeIcon(mode);
		Icons.DrawIcon(icon);
		ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X);
		ImGui.Text($"{mode} Mode");
	}

	private FontAwesomeIcon GetModeIcon(EditMode mode) => mode switch {
		EditMode.Object => FontAwesomeIcon.VectorSquare,
		EditMode.Pose => FontAwesomeIcon.CircleNodes,
		_ => FontAwesomeIcon.None
	};
	
	// State frame for actor, select, overlay
	
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
		
		var ct = this._sceneMgr.Editor.Selection.Count;
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
		
		var overlay = this._cfg.Config.Overlay_Visible;
		var btnIcon = overlay ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		if (Buttons.DrawIconButtonHint(btnIcon, "Toggle screen overlay", btnSize))
			this._cfg.Config.Overlay_Visible = !overlay;
	}
	
	// Tree buttons

	private void DrawTreeButtons() {
		Buttons.DrawIconButton(FontAwesomeIcon.Plus);
		ImGui.SameLine();
		Buttons.DrawIconButton(FontAwesomeIcon.Filter);
	}
}
