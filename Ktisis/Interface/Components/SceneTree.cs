using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Editing;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Scene.Objects.World;
using Ktisis.Interface.Helpers;
using Ktisis.Interface.Widgets;
using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Display;
using Ktisis.Data.Config;

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
		DrawTree(scene, scene.GetChildren());
	}

	private void DrawTree(SceneGraph scene, IEnumerable<SceneObject> objects) {
		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 6f });
		foreach (var node in objects)
			DrawNode(scene, node);
		ImGui.PopStyleVar();
	}

	private void DrawNode(SceneGraph scene, SceneObject item) {
		var pos = ImGui.GetCursorPosY();
		var isVisible = pos > this.MinY && pos < this.MaxY;

		var display = this._cfg.GetItemDisplay(item.ItemType);

		var children = item.GetChildren();
		
		var isLeaf = children.Count == 0;
		var flags = ImGuiTreeNodeFlags.AllowItemOverlap | ImGuiTreeNodeFlags.SpanFullWidth | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow);

		if (isVisible) {
			if (item.Flags.HasFlag(ObjectFlags.Selected))
				flags |= ImGuiTreeNodeFlags.Selected;
			var hover = ImGui.GetColorU32(ImGuiCol.HeaderHovered);
			ImGui.PushStyleColor(ImGuiCol.HeaderActive, hover);
			ImGui.PushStyleColor(ImGuiCol.Text, display.Color);
		}

		var start = ImGui.GetCursorPosX();
		var expand = ImGui.TreeNodeEx($"##{item.UiId}", flags);
		
		if (isVisible) {
			ImGui.SameLine();
			DrawLabel(scene, item, display, isLeaf, start);
			ImGui.PopStyleColor(2);
		}
		
		if (!expand) return;
		
		if (!isLeaf)
			DrawTree(scene, children);
		ImGui.TreePop();
	}

	private void DrawLabel(SceneGraph scene, SceneObject item, ItemDisplay display, bool isLeaf, float start) {
		var hasIcon = display.Icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(display.Icon).X / 2 : 0;
		var iconSpace = hasIcon ? UiBuilder.IconFont.FontSize : 0;

		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + (iconSpace / 2) - iconPadding);
		
		// Visibility toggle

		var rightAdjust = 0.0f;
		if (item is IVisibility vis) {
			var disabled = !this._sceneMgr.Editor.IsItemInfluenced(item);
			rightAdjust = DrawVisibility(scene, vis, display, disabled);
		}

		// Icon + Name

		ImGui.SetCursorPosX(cursor);
		Icons.DrawIcon(display.Icon);
		ImGui.SameLine();

		var spacing = ImGui.GetStyle().ItemSpacing.X + iconSpace;
		cursor += spacing;
		ImGui.SetCursorPosX(cursor);

		var labelAvail = ImGui.GetContentRegionAvail().X - rightAdjust;
		ImGui.Text(item.Name.FitToWidth(labelAvail));
		HandleClick(scene, item, start, isLeaf, labelAvail + spacing);
	}

	private float DrawVisibility(SceneGraph scene, IVisibility item, ItemDisplay display, bool disabled = false) {
		const FontAwesomeIcon icon = FontAwesomeIcon.Eye;
		var size = Icons.CalcIconSize(icon);
		var spacing = ImGui.GetStyle().ItemSpacing.X;

		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - size.X - spacing);
		var alpha = disabled ? 0x3A : (item.Visible ? 0xFF : 0x90);
		
		var color = display.Color.SetAlpha((byte)alpha);
		ImGui.PushStyleColor(ImGuiCol.Text, color);
		Icons.DrawIcon(icon);
		ImGui.PopStyleColor();
		
		if (!disabled && Buttons.IsClicked())
			item.ToggleVisible();
		
		ImGui.SameLine();
		
		return size.X + spacing * 2;
	}
	
	private void HandleClick(SceneGraph scene, SceneObject item, float start, bool isLeaf, float avail) {
		var size = ImGui.GetItemRectSize();
		var cursor = ImGui.GetCursorScreenPos()
			.Sub(ImGui.GetCursorPosX(), ImGui.GetStyle().ItemSpacing.Y + size.Y);

		var nodeSpace = ImGui.GetTreeNodeToLabelSpacing();
		
		var min_r = cursor with { X = cursor.X + start + nodeSpace };
		var max_r = size with { X = avail };
		var isHover = ImGui.IsMouseHoveringRect(min_r, min_r + max_r);
		if (!isHover) {
			var max_l = size with { X = start + (isLeaf ? nodeSpace : 0) };
			isHover |= ImGui.IsMouseHoveringRect(cursor, cursor + max_l);
		}

		if (!isHover || !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			return;

		var flags = GuiSelect.GetSelectFlags();
		this._sceneMgr.Editor.Selection.HandleClick(item, flags);
	}
}
