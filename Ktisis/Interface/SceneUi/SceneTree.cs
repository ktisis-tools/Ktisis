using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Scenes;
using Ktisis.Scenes.Objects;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;
using Ktisis.Scenes.Objects.Impl;

namespace Ktisis.Interface.SceneUi; 

public class SceneTree {
	// Singleton access
	
	private readonly SceneManager? SceneManager;
	
	// Constructor

	public SceneTree(SceneManager? scene) {
		SceneManager = scene;
	}
	
	// Exposed draw method
	
	public void Draw(float height) {
		ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, ImGui.GetFontSize());

		var isActive = SceneManager?.Scene != null;
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

	private void DrawSceneRoot() {
		if (SceneManager?.Scene is not Scene scene) return;
		
		SelectFlags = SelectFlags.Single;
		if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
			SelectFlags |= SelectFlags.Multiple;
		if (ImGui.IsKeyDown(ImGuiKey.ModShift))
			SelectFlags |= SelectFlags.Range;
		
		SelectRange.Clear();
		
		PreCalc();
		DrawTree(scene.Children);
	}

	private void DrawTree(List<SceneObject> objects) {
		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 6f });
		objects.ForEach(DrawNode);
		ImGui.PopStyleVar();
	}

	private void DrawNode(SceneObject item) {
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
		
		if (SelectFlags.HasFlag(SelectFlags.Range))
			CollectRange(item);
		
		if (isVisible) {
			ImGui.SameLine();
			HandleSelect(item);
			DrawLabel(item);
			ImGui.PopStyleColor();
		}

		if (!expand) return;
		
		if (!isLeaf)
			DrawTree(item.Children);
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
	
	private SelectFlags SelectFlags;

	private string? SelectRangeStart;
	
	private readonly List<SceneObject> SelectRange = new();

	private void HandleSelect(SceneObject item) {
		var min = ImGui.GetCursorScreenPos();
		var max = min + ImGui.GetItemRectSize() with {
			X = ImGui.GetContentRegionAvail().X
		};

		if (!ImGui.IsMouseHoveringRect(min, max)) return;
		
		if (SelectFlags.HasFlag(SelectFlags.Range)
			? !ImGui.IsMouseDown(ImGuiMouseButton.Left)
			: !ImGui.IsMouseClicked(ImGuiMouseButton.Left)
		) return;
		
		SceneManager!.UserSelect(item, SelectFlags);

		if (!SelectFlags.HasFlag(SelectFlags.Range) || SceneManager.SelectCursor is not string tar)
			return;

		if (tar != item.UiId && !SelectFromRange(tar))
			SelectRangeStart = item.UiId;
	}

	private void CollectRange(SceneObject item) {
		if (SceneManager?.SelectCursor == null) return;
		
		SelectRange.Add(item);

		if (SelectRangeStart == null || item.UiId != SceneManager.SelectCursor)
			return;
		
		SelectFromRange(SelectRangeStart);
		SelectRangeStart = null;
	}
	
	private bool SelectFromRange(string id) {
		var index = SelectRange.FindIndex(x => x.UiId == id);
		if (index == -1)
			return false;

		SceneManager?.Scene?.UnselectAll();

		var range = SelectRange.Take(Range.StartAt(index));
		foreach (var tar in range)
			SceneManager!.AddSelection(tar);
		
		return true;
	}
}