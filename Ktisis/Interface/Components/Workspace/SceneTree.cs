using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Editor.Selection;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Interface.Components.Workspace;

public class SceneTree {
	private readonly IEditorContext _ctx;
	
	private readonly SceneDragDropHandler _dragDrop;
	private List<SceneEntity> _nodes;
	private SceneEntity? _shiftNode;
	private int? _originIndex;
	
	public SceneTree(
		IEditorContext ctx
	) {
		this._ctx = ctx;
		this._dragDrop = new SceneDragDropHandler(ctx);
		this._nodes = new List<SceneEntity>();
		this._shiftNode = null;
		this._originIndex = null;
	}
	
	// Draw frame
    
	public void Draw(float height) {
		var frame = false;
		try {
			var id = ImGui.GetID("SceneTree_Frame");
			frame = ImGui.BeginChildFrame(id, new Vector2(-1, height));
			if (!frame) return;
			this.DrawScene(height);
		} catch (Exception err) {
			Ktisis.Log.Error($"Error drawing scene tree:\n{err}");
		} finally {
			if (frame) ImGui.EndChildFrame();
		}
	}
	
	// Draw scene entities

	private static float IconSpacing => UiBuilder.DefaultFontSizePx * ImGuiHelpers.GlobalScale;

	private float MinY;
	private float MaxY;

	private void PreCalc(float height) {
		var scroll = ImGui.GetScrollY();
		this.MinY = scroll - ImGui.GetFrameHeight();
		this.MaxY = height + scroll;
	}

	private void DrawScene(float height) {
		this._nodes.Clear();
		this._shiftNode = null;
		this.PreCalc(height);

		var spacing = ImGui.GetStyle().ItemSpacing;
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing with { Y = 5.0f });
		this.IterateTree(this._ctx.Scene.Children);

		// handle any shift-clicked node using target node & ordered tree list
		if (this._shiftNode != null) this.ResolveShiftSelect();
	}

	private void IterateTree(IEnumerable<SceneEntity> entities) {
		try {
			ImGui.TreePush(nint.Zero);
			foreach (var item in entities) {
				this._nodes.Add(item); // put each iterated node in order on the shared list
				this.DrawNode(item, out var shiftClicked);
				if (shiftClicked) {
					this._shiftNode = item; // while recursing, store if a node was shift-clicked to handle after root finishes
				}
			}
		} finally {
			ImGui.TreePop();
		}
	}
	
	// Nodes

	private enum TreeNodeFlag {
		Leaf,
		Expand,
		Collapse
	}

	private void DrawNode(SceneEntity node, out bool shiftClicked) {
		shiftClicked = false;
		var pos = ImGui.GetCursorPos();
		var isRender = pos.Y > this.MinY && pos.Y < this.MaxY;

		var id = $"##SceneTree_{node.GetHashCode():X}";

		const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns;
		ImGui.Selectable(id, node.IsSelected, selectFlags);

		var isHover = ImGui.IsWindowHovered();
		var size = ImGui.GetItemRectSize();
		
		// NOTE: This blocks ImGUi.IsWindowHovered for some stupid reason
		this._dragDrop.Handle(node);

		var imKey = ImGui.GetID(id);
		var state = ImGui.GetStateStorage();
		
		var isExpand = state.GetBool(imKey);

		var children = node.Children.ToList();
		
		if (isRender) {
			var flag = isExpand switch {
				_ when children.Count is 0 => TreeNodeFlag.Leaf,
				_ when node is EntityPose => TreeNodeFlag.Leaf,
				true => TreeNodeFlag.Expand,
				false => TreeNodeFlag.Collapse
			};

			var rightAdjust = this.DrawButtons(node, isHover);
			if (this.DrawNodeLabel(node, pos, flag, rightAdjust))
				state.SetBool(imKey, isExpand = !isExpand);

			if (isHover && this.IsNodeHovered(pos, size, rightAdjust)) {
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
					this._ctx.Interface.OpenEditorFor(node);
				} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
					// if we shift-click, handle the multi-select for this node later after tree rendering
					if (ImGui.IsKeyDown(ImGuiKey.ModShift)) shiftClicked = true;
					else {
						var mode = GuiHelpers.GetSelectMode();
						node.Select(mode);
					}
				} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
					this._ctx.Interface.OpenSceneEntityMenu(node);
				}

				if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !shiftClicked)
					this._originIndex = this._nodes.Count - 1;
			}
		}

		if (isExpand || node is EntityPose) this.IterateTree(children);
	}

	private bool DrawNodeLabel(SceneEntity item, Vector2 pos, TreeNodeFlag flag, float rightAdjust = 0.0f) {
		var display = this._ctx.Config.GetEntityDisplay(item);

        // Caret

		var expand = false;
		var style = ImGui.GetStyle();
		ImGui.SameLine();
		ImGui.SetCursorPosX(pos.X - style.ItemSpacing.X);
		if (item is not EntityPose)
			expand = this.DrawNodeCaret(display.Color, flag);

		// Icon + Label

		using var _ = ImRaii.PushColor(ImGuiCol.Text, display.Color);
		this.DrawNodeIcon(display.Icon);

		var avail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(avail - rightAdjust));

		return expand;
	}

	private bool DrawNodeCaret(uint color, TreeNodeFlag flag) {
		var cursor = ImGui.GetCursorPosX();

		var caretIcon = flag switch {
			TreeNodeFlag.Collapse => FontAwesomeIcon.CaretRight,
			TreeNodeFlag.Expand => FontAwesomeIcon.CaretDown,
			_ => FontAwesomeIcon.None
		};

		using (ImRaii.PushColor(ImGuiCol.Text, color.SetAlpha(0xCF)))
			Icons.DrawIcon(caretIcon);

		ImGui.SameLine();

		var spacing = ImGui.GetStyle().ItemInnerSpacing;
		cursor += spacing.X + IconSpacing;
		ImGui.SetCursorPosX(cursor);

		return ButtonsEx.IsClicked();
	}

	private void DrawNodeIcon(FontAwesomeIcon icon) {
		var hasIcon = icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(icon).X / 2.0f : 0.0f;
		var iconSpace = hasIcon ? IconSpacing : 0;
		Icons.DrawIcon(icon);
		ImGui.SameLine(0, iconSpace - iconPadding);
	}
	
	// Buttons

	private float DrawButtons(SceneEntity node, bool isHover) {
		var initial = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
		var cursor = initial;

		this.DrawVisibilityButton(node, ref cursor, isHover);
		if (node is IAttachable attach)
			this.DrawAttachButton(attach, ref cursor, isHover);
		if (node is IHideable hideable)
			this.DrawHideButton(hideable, ref cursor, isHover);

		return initial - cursor;
	}

	private void DrawVisibilityButton(SceneEntity node, ref float cursor, bool isHover) {
		if (node is not IVisibility vis) return;

		var isActive = this._ctx.Config.Overlay.Visible;
		
		var isVisible = vis.Visible;
		var color = isVisible ? 0xEFFFFFFF : 0x80FFFFFF;
		if (!isActive)
			color = color.SetAlpha((byte)(isVisible ? 0x60 : 0x30));

		var icon = vis is WorldEntity ? FontAwesomeIcon.LocationCrosshairs : FontAwesomeIcon.Eye;
		if (this.DrawButton(ref cursor, icon, color) && isHover)
			vis.Toggle();
	}

	private void DrawAttachButton(IAttachable attach, ref float cursor, bool isHover) {
		if (!attach.IsAttached()) return;
		
		if (this.DrawButton(ref cursor, FontAwesomeIcon.Link, 0xFFFFFFFF) && isHover)
			this._ctx.Posing.Attachments.Detach(attach);

		if (!isHover || !ImGui.IsItemHovered()) return;

		var bone = attach.GetParentBone();
		var name = bone != null ? this._ctx.Locale.GetBoneName(bone) : "UNKNOWN";
		using var _ = ImRaii.Tooltip();
		ImGui.Text($"Attached to {name}");
		ImGui.Text($"Click to reset attachment\nClick+Drag to set new attachment");
	}

	private void DrawHideButton(IHideable entity, ref float cursor, bool isHover) {
		var color = entity.IsHidden ? 0x80FFFFFF : 0xEFFFFFFF;
		if(this.DrawButton(ref cursor, FontAwesomeIcon.Mask, color) && isHover)
			entity.ToggleHidden();

		if (!isHover || !ImGui.IsItemHovered()) return;
		using var _ = ImRaii.Tooltip();
		ImGui.Text(entity.IsHidden ? "Unhide Entity" : "Hide Entity");
	}

	private bool DrawButton(ref float cursor, FontAwesomeIcon icon, uint? color = null) {
		cursor -= (Icons.CalcIconSize(icon).X / ImGuiHelpers.GlobalScale) + ImGui.GetStyle().ItemSpacing.X;
		ImGui.SameLine();
		ImGui.SetCursorPosX(cursor);
		using var _ = ImRaii.PushColor(ImGuiCol.Text, color ?? 0, color.HasValue);
		Icons.DrawIcon(icon);
		return ButtonsEx.IsClicked();
	}
	
	// Handle hover + click

	private bool IsNodeHovered(Vector2 pos, Vector2 size, float rightAdjust) {
		var pad = ImGui.GetStyle().ItemSpacing.X;
		var min = ImGui.GetWindowPos() + pos.AddX(pad).SubY(ImGui.GetScrollY() + 2);
		var max = min.Add(size.X - pos.X - pad - rightAdjust, size.Y);
		return ImGui.IsMouseHoveringRect(min, max);
	}

	// Handle shift-click multiselect
	private void ResolveShiftSelect() {
		// user has shiftclicked a SceneEntity _shiftNode at indexTarget in _nodes
		var indexTarget = this._nodes.IndexOf(this._shiftNode!);
		if (indexTarget < 0) return; // shift-clicked node was not in recursed _nodes list

		// find the index of the most recently selected entity that's also present in _nodes (at indexOrigin),
		if (this._originIndex == null || this._originIndex == indexTarget) return;
		if (this._originIndex >= this._nodes.Count) this._originIndex = this._nodes.Count - 1; // list has shrunk since last originindex, reset to last entry

		// and multi-select every node between indexOrigin (exclusive) and indexTarget (inclusive)
		if (this._originIndex > indexTarget) {
			for (var i = indexTarget; i < this._originIndex; i++)
				if (this._nodes[i] is { IsSelected: false } node) node.Select(SelectMode.Multiple); // moving DOWN the tree
		} else {
			for (var i = indexTarget; i > this._originIndex; i--)
				if (this._nodes[i] is { IsSelected: false } node) node.Select(SelectMode.Multiple); // moving UP the tree
		}

		this._originIndex = indexTarget;
	}
}
