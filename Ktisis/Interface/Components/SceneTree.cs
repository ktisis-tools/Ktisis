using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Interface.Widgets;
using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Editing;

namespace Ktisis.Interface.Components;

public delegate void OnItemClickedHandler(SceneObject item, SelectFlags flags);

public class SceneTree {
	// Constructor

	private readonly ConfigService _cfg;

	private readonly SceneManager _sceneMgr;

	public SceneTree(ConfigService _cfg, SceneManager _sceneMgr) {
		this._cfg = _cfg;
		this._sceneMgr = _sceneMgr;
	}
	
	// Events

	public event OnItemClickedHandler? OnItemClicked;
	
	// UI draw

	public void Draw(float height) {
		var active = this._sceneMgr.IsActive;
		ImGui.BeginDisabled(!active);
		if (ImGui.BeginChildFrame(101, -Vector2.One with { Y = height })) {
			try {
				DrawSceneRoot(height);
			} catch (Exception err) {
				PluginLog.Error($"Error drawing scene tree:\n{err}");
			} finally {
				ImGui.EndChildFrame();
			}
		}
		ImGui.EndDisabled();
	}

	// Draw frame

	private static float IconSpacing => UiBuilder.IconFont.FontSize;

	private float MinY;
	private float MaxY;

	private void PreCalc(float height) {
		var scroll = ImGui.GetScrollY();
		this.MinY = scroll - ImGui.GetFrameHeight();
		this.MaxY = height + scroll;
	}

	private void DrawSceneRoot(float height) {
		var scene = this._sceneMgr.Scene;
		if (scene is null) return;

		PreCalc(height);

		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 5.0f });
		IterateTree(scene, scene.GetChildren());
		ImGui.PopStyleVar();
	}

	private void IterateTree(SceneGraph scene, IEnumerable<SceneObject> items) {
		ImGui.TreePush();
		foreach (var item in items)
			DrawNode(scene, item);
		ImGui.TreePop();
	}

	// Node

	private enum TreeNodeState {
		Leaf = 0,
		Expanded = 1,
		Collapsed = 2
	}

	private void DrawNode(SceneGraph scene, SceneObject item) {
		var pos = ImGui.GetCursorPos();
		var isRender = pos.Y > this.MinY && pos.Y < this.MaxY;

		var id = $"##SceneTree_{item.UiId}";

		const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns;
		var isClick = ImGui.Selectable(id, item.IsSelected(), selectFlags);

		var size = ImGui.GetItemRectSize();

		var imKey = ImGui.GetID(id);
		var state = ImGui.GetStateStorage();

		var isExpand = state.GetBool(imKey);

		var children = item.GetChildren();

		if (isRender) {
			var flags = isExpand switch {
				_ when children.Count == 0 => TreeNodeState.Leaf,
				true => TreeNodeState.Expanded,
				false => TreeNodeState.Collapsed
			};

			var buttons = GetButtonFlags(item);
			var rightAdjust = DrawButtons(item, buttons);

			if (DrawNodeLabel(scene, item, pos, flags, rightAdjust))
				state.SetBool(imKey, isExpand = !isExpand);

			if (isClick && IsNodeHovered(pos, size, rightAdjust))
				ClickItem(item);
		}

		if (isExpand) IterateTree(scene, children);
	}

	private void ClickItem(SceneObject item) {
		var clickFlags = GuiHelpers.GetSelectFlags();
		this.OnItemClicked?.Invoke(item, clickFlags);
	}

	private bool DrawNodeLabel(SceneGraph scene, SceneObject item, Vector2 pos, TreeNodeState state, float rightAdjust = 0.0f) {
		var display = this._cfg.GetItemDisplay(item.ItemType);

		// Caret

		var style = ImGui.GetStyle();

		ImGui.SameLine();
		ImGui.SetCursorPosX(pos.X - style.ItemSpacing.X);

		var expand = DrawNodeCaret(display.Color, state);

		// Icon + Label

		ImGui.PushStyleColor(ImGuiCol.Text, display.Color);
		DrawNodeIcon(display.Icon);
		var avail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(avail - rightAdjust));
		ImGui.PopStyleColor();

		return expand;
	}

	private bool DrawNodeCaret(uint color, TreeNodeState state) {
		var cursor = ImGui.GetCursorPosX();

		ImGui.PushStyleColor(ImGuiCol.Text, color.SetAlpha(0xCF));
		var caretIcon = state switch {
			TreeNodeState.Collapsed => FontAwesomeIcon.CaretRight,
			TreeNodeState.Expanded => FontAwesomeIcon.CaretDown,
			_ => FontAwesomeIcon.None
		};
		Icons.DrawIcon(caretIcon);
		ImGui.PopStyleColor();

		ImGui.SameLine();
		cursor += ImGui.GetStyle().ItemInnerSpacing.X + IconSpacing;
		ImGui.SetCursorPosX(cursor);

		return Buttons.IsClicked();
	}

	private void DrawNodeIcon(FontAwesomeIcon icon) {
		var hasIcon = icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(icon).X / 2 : 0;
		var iconSpace = hasIcon ? IconSpacing : 0;

		Icons.DrawIcon(icon);
		ImGui.SameLine(0, iconSpace - iconPadding);
	}

	// Buttons

	[Flags]
	private enum NodeButtonFlags {
		None = 0,
		Visibility = 1,
		Attachment = 2,
		IkHandle = 4
	}

	private NodeButtonFlags GetButtonFlags(SceneObject item) {
		var flags = NodeButtonFlags.None;
		if (item is IVisibility)
			flags |= NodeButtonFlags.Visibility;
		// TODO: Character -> Attach
		// TODO: ArmatureGroup -> IK
		return flags;
	}

	private int GetButtonCount(NodeButtonFlags f)
		=> Enum.GetValues<NodeButtonFlags>()
			.Count(v => v != 0 && f.HasFlag(v));

	private float DrawButtons(SceneObject item, NodeButtonFlags flags) {
		var initial = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
		var cursor = initial;

		if (flags.HasFlag(NodeButtonFlags.Visibility) && this._cfg.Config.Overlay_Visible) {
			var iVis = (IVisibility)item;
			if (DrawButton(ref cursor, FontAwesomeIcon.Eye, iVis.Visible ? 0xFFFFFFFF : 0x90FFFFFF))
				iVis.ToggleVisible();
		}

		// IK
		//DrawButton(ref cursor, FontAwesomeIcon.CodeFork, 0xFF5F56FF);

		// Attach
		//DrawButton(ref cursor, FontAwesomeIcon.Link, 0xFF88D154);

		return initial - cursor;
	}

	private bool DrawButton(ref float cursor, FontAwesomeIcon icon, uint? color = null) {
		cursor -= Icons.CalcIconSize(icon).X + ImGui.GetStyle().ItemSpacing.X;
		ImGui.SameLine();
		ImGui.SetCursorPosX(cursor);
		if (color != null)
			ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
		Icons.DrawIcon(icon);
		if (color != null)
			ImGui.PopStyleColor();
		return Buttons.IsClicked();
	}

	// Handle click

	private bool IsNodeHovered(Vector2 pos, Vector2 size, float rightAdjust) {
		var pad = ImGui.GetStyle().ItemSpacing.X;
		var min = ImGui.GetWindowPos() + pos.AddX(pad).SubY(ImGui.GetScrollY() + 2);
		var max = min.Add(size.X - pos.X - pad - rightAdjust, size.Y);
		return ImGui.IsMouseHoveringRect(min, max);
	}
}
