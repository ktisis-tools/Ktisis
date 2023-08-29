using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Data;
using Ktisis.Data.Config;
using Ktisis.Scene;
using Ktisis.Scene.Objects;
using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Display;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components; 

public class SceneTree {
	// Constructor
	
	private readonly ConfigFile _cfg;
	
	private readonly SceneManager _sceneMgr;

	public SceneTree(ConfigFile _cfg, SceneManager _scene) {
		this._cfg = _cfg;
		this._sceneMgr = _scene;
	}
	
	// Exposed draw method
	
	public void Draw(float height) {
		ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, ImGui.GetFontSize());

		var isActive = this._sceneMgr.Scene != null;
		ImGui.BeginDisabled(!isActive);
		if (DrawFrame(height)) {
			try {
				if (isActive)
					DrawSceneRoot();
			} catch (Exception e) {
				PluginLog.Error($"Error while drawing scene tree:\n{e}");
			} finally {
				ImGui.EndChildFrame();
			}
		}
		ImGui.EndDisabled();
		
		ImGui.PopStyleVar();
	}

	// Draw outer frame
	
	private float FrameHeight;

	private bool DrawFrame(float height) {
		this.FrameHeight = height;
		return ImGui.BeginChildFrame(101, new Vector2(-1, this.FrameHeight));
	}
	
	// Draw tree
	
	private float MinY;
	private float MaxY;

	private void PreCalc() {
		var scroll = ImGui.GetScrollY();
		this.MinY = scroll - ImGui.GetFrameHeight();
		this.MaxY = this.FrameHeight + scroll;
	}

	private void DrawSceneRoot() {
		if (this._sceneMgr.Scene is not SceneGraph scene) return;
		
		PreCalc();
		DrawTree(scene.GetChildren());
	}

	private void DrawTree(IEnumerable<SceneObject> objects) {
		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 6f });
		foreach (var node in objects)
			DrawNode(node);
		ImGui.PopStyleVar();
	}

	private void DrawNode(SceneObject item) {
		var pos = ImGui.GetCursorPosY();
		var isVisible = pos > this.MinY && pos < this.MaxY;

		var display = this._cfg.GetItemDisplay(item.ItemType);

		var children = item.GetChildren();
		
		var isLeaf = children.Count == 0;
		var flags = ImGuiTreeNodeFlags.SpanAvailWidth | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow);

		if (isVisible) {
			// TODO
			//if (item.Selected)
				//flags |= ImGuiTreeNodeFlags.Selected;
			ImGui.PushStyleColor(ImGuiCol.Text, display.Color);
		}
		
		var expand = ImGui.TreeNodeEx($"##{item.UiId}", flags);
		
		//if (SelectFlags.HasFlag(SelectFlags.Range))
			//CollectRange(item);
		
		if (isVisible) {
			ImGui.SameLine();
			DrawLabel(item, display);
			ImGui.PopStyleColor();
		}

		if (!expand) return;
		
		if (!isLeaf)
			DrawTree(children);
		ImGui.TreePop();
	}

	private void DrawLabel(SceneObject item, ItemDisplay display) {
		var hasIcon = display.Icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(display.Icon).X / 2 : 0;
		var iconSpace = hasIcon ? UiBuilder.IconFont.FontSize : 0;

		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + (iconSpace / 2) - iconPadding);

		// Icon + Name
        
		Icons.DrawIcon(display.Icon);
		ImGui.SameLine();

		cursor += ImGui.GetStyle().ItemSpacing.X + iconSpace;
		ImGui.SetCursorPosX(cursor);

		var labelAvail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(labelAvail));
	}
}