using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Scenes;
using Ktisis.Scenes.Objects;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components; 

public class ItemTree {
	// Public draw methods
	
	public void Draw(Scene? scene) {
		ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, ImGui.GetFontSize());
		
		var isActive = scene != null;
		ImGui.BeginDisabled(!isActive);
		if (DrawFrame()) {
			if (isActive)
				DrawTree(scene!.Children);
			else
				ImGui.Text("Waiting for scene...");
			ImGui.EndChildFrame();
		}
		ImGui.EndDisabled();
		
		ImGui.PopStyleVar();
	}

	// Draw outer frame
	
	private float FrameHeight;

	private bool DrawFrame() {
		var avail = ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().FramePadding.Y * 2;
		FrameHeight = avail - 10;
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

	private void DrawTree(List<SceneObject> objects) {
		PreCalc();
		var spacing = ImGui.GetStyle().ItemSpacing;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, spacing with { Y = 6f });
		objects.ForEach(DrawNode);
		ImGui.PopStyleVar();
	}

	private void DrawNode(SceneObject item) {
		var pos = ImGui.GetCursorPosY();
		var isVisible = pos > MinY && pos < MaxY;

		var color = item.Color;
		
		var isLeaf = item.Children == null || item.Children.Count == 0;
		var flags = ImGuiTreeNodeFlags.SpanAvailWidth | (isLeaf ? ImGuiTreeNodeFlags.Leaf : ImGuiTreeNodeFlags.OpenOnArrow);
		
		if (isVisible) ImGui.PushStyleColor(ImGuiCol.Text, color);
		var expand = ImGui.TreeNodeEx($"##{item.UiId}", flags);
		if (isVisible) {
			DrawLabel(item);
			ImGui.PopStyleColor();
		}

		if (expand) {
			if (!isLeaf)
				DrawTree(item.Children!);
			ImGui.TreePop();
		}
	}

	private void DrawLabel(SceneObject item) {
		var hasIcon = item.Icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(item.Icon).X / 2 : 0;
		var iconSpace = hasIcon ? UiBuilder.IconFont.FontSize : 0;

		ImGui.SameLine();

		var cursor = ImGui.GetCursorPosX();
		ImGui.SetCursorPosX(cursor + (iconSpace / 2) - iconPadding);

		// Icon + Name

		Icons.DrawIcon(item.Icon);
		ImGui.SameLine();

		cursor += ImGui.GetStyle().ItemSpacing.X + iconSpace;
		ImGui.SetCursorPosX(cursor);

		var labelAvail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(labelAvail));

		// Visibility Toggle
		// TODO: Reimplement this later.
		/*ImGui.SameLine();
		var togglePos = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Icons.CalcIconSize(FontAwesomeIcon.Eye).X * 1.25f;
		ImGui.PushStyleColor(ImGuiCol.Text, item.Color.SetAlpha(0x90));
		//ImGui.SameLine(togglePos);
		ImGui.SetCursorPosX(togglePos);
		Icons.DrawIcon(FontAwesomeIcon.Eye); // TODO: Button
		ImGui.PopStyleColor();*/
	}
}