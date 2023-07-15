using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Scenes;
using Ktisis.Scenes.Objects;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.SceneUi;

public class SceneTree {
	// Public draw methods

	public void Draw(Scene? scene, float height) {
		ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, ImGui.GetFontSize());

		var isActive = scene != null;
		ImGui.BeginDisabled(!isActive);
		if (DrawFrame(height)) {
			if (isActive)
				DrawSceneRoot(scene!);
			else
				ImGui.Text("Waiting for scene...");
			ImGui.EndChildFrame();
		}
		ImGui.EndDisabled();

		ImGui.PopStyleVar();
	}

	// Draw outer frame

	private float FrameHeight;

	private bool DrawFrame(float height) {
		FrameHeight = height;
		return ImGui.BeginChildFrame(101, new Vector2(-1, FrameHeight));
	}

	// Draw tree

	private float MinY;
	private float MaxY;

	private void PreCalc() {
		var scroll = ImGui.GetScrollY();
		MinY = scroll - ImGui.GetFrameHeight();
		MaxY = FrameHeight + scroll;
	}

	private void DrawSceneRoot(Scene scene) {
		SelectFlags = SelectEnum.None;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			SelectFlags |= SelectEnum.SelectCtrl;
		if (ImGui.IsKeyDown(ImGuiKey.ModShift))
			SelectFlags |= SelectEnum.SelectRange;

		SelectTarget = null;
		SelectStack.Clear();

		PreCalc();
		DrawTree(scene, scene.Children);
	}

	private void DrawTree(Scene scene, List<SceneObject> objects) {
		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 6f });
		objects.ForEach(item => DrawNode(scene, item));
		ImGui.PopStyleVar();
	}

	private void DrawNode(Scene scene, SceneObject item) {
		var pos = ImGui.GetCursorPosY();
		var isVisible = pos > MinY && pos < MaxY;

		var color = item.Color;

		var isLeaf = item.Children.Count == 0;
		var flags = ImGuiTreeNodeFlags.SpanAvailWidth | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow);

		if (isVisible) {
			if (item.Selected)
				flags |= ImGuiTreeNodeFlags.Selected;
			ImGui.PushStyleColor(ImGuiCol.Text, color);
		}

		var expand = ImGui.TreeNodeEx($"##{item.UiId}", flags);

		HandleRangeSelectMode(scene, item);

		if (isVisible) {
			ImGui.SameLine();
			HandleSelect(scene, item);
			DrawLabel(item);
			ImGui.PopStyleColor();
		}

		if (!expand) return;

		if (!isLeaf)
			DrawTree(scene, item.Children);
		ImGui.TreePop();
	}

	private void DrawLabel(SceneObject item) {
		var hasIcon = item.Icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(item.Icon).X / 2 : 0;
		var iconSpace = hasIcon ? UiBuilder.IconFont.FontSize : 0;

		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + (iconSpace / 2) - iconPadding);

		// Icon + Name

		Icons.DrawIcon(item.Icon);
		ImGui.SameLine();

		cursor += ImGui.GetStyle().ItemSpacing.X + iconSpace;
		ImGui.SetCursorPosX(cursor);

		var labelAvail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(labelAvail));
	}

	// Selection handling

	[Flags]
	private enum SelectEnum {
		None = 0,
		SelectCtrl = 1,
		SelectRange = 2
	}

	private readonly List<SceneObject> SelectStack = new();

	private SelectEnum SelectFlags;
	private string? SelectCursor;
	private string? SelectTarget;

	private void HandleSelect(Scene scene, SceneObject item) {
		var min = ImGui.GetCursorScreenPos();
		var max = min + ImGui.GetItemRectSize() with {
			X = ImGui.GetContentRegionAvail().X
		};

		if (!ImGui.IsMouseHoveringRect(min, max)) return;

		if (SelectCursor != null && SelectFlags.HasFlag(SelectEnum.SelectRange) && ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
			// Handle item range selection
			item.Select();
			if (!SelectFromStack(scene, SelectCursor))
				SelectTarget = item.UiId;
		} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
			// Handle individual item selection
			if (SelectFlags.HasFlag(SelectEnum.SelectCtrl)) {
				item.ToggleSelected();
			} else {
				var select = item.Selected;
				var unselectMul = scene.UnselectAll() > 1;
				item.SetSelected(unselectMul || !select);
			}
			SelectCursor = item.Selected ? item.UiId : null;
		}
	}

	private bool SelectFromStack(Scene scene, string id) {
		var index = SelectStack.FindIndex(x => x.UiId == id);
		if (index == -1)
			return false;

		scene.UnselectAll();

		var range = SelectStack.Take(Range.StartAt(index));
		foreach (var tar in range)
			tar.Select();

		return true;
	}

	private void HandleRangeSelectMode(Scene scene, SceneObject item) {
		if (!SelectFlags.HasFlag(SelectEnum.SelectRange) || SelectCursor == null)
			return;

		SelectStack.Add(item);

		if (item.UiId != SelectCursor) return;

		if (!item.Selected) {
			SelectCursor = null;
		} else if (SelectTarget != null) {
			SelectFromStack(scene, SelectTarget);
			SelectTarget = null;
		}
	}
}
