using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Scene.Objects;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components; 

public class SceneTree {
	// Constructor
	
	private readonly SceneManager _sceneMgr;

	public SceneTree(SceneManager _scene) {
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

		//var color = item.Color;
		// TODO
		var color = 0xFFFFFFFF;

		var children = item.GetChildren();
		
		var isLeaf = children.Count == 0;
		var flags = ImGuiTreeNodeFlags.SpanAvailWidth | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow);

		if (isVisible) {
			// TODO
			//if (item.Selected)
				//flags |= ImGuiTreeNodeFlags.Selected;
			ImGui.PushStyleColor(ImGuiCol.Text, color);
		}
		
		var expand = ImGui.TreeNodeEx($"##{item.UiId}", flags);
		
		//if (SelectFlags.HasFlag(SelectFlags.Range))
			//CollectRange(item);
		
		if (isVisible) {
			ImGui.SameLine();
			DrawLabel(item);
			ImGui.PopStyleColor();
		}

		if (!expand) return;
		
		if (!isLeaf)
			DrawTree(children);
		ImGui.TreePop();
	}

	private void DrawLabel(SceneObject item) {
		// TODO
		//var hasIcon = item.Icon != FontAwesomeIcon.None;
		var hasIcon = false;
		var iconPadding = hasIcon ? Icons.CalcIconSize(FontAwesomeIcon.None).X / 2 : 0;
		var iconSpace = hasIcon ? UiBuilder.IconFont.FontSize : 0;

		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + (iconSpace / 2) - iconPadding);

		// Icon + Name

		// TODO
		Icons.DrawIcon(FontAwesomeIcon.None);
		ImGui.SameLine();

		cursor += ImGui.GetStyle().ItemSpacing.X + iconSpace;
		ImGui.SetCursorPosX(cursor);

		var labelAvail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(labelAvail));
	}
}