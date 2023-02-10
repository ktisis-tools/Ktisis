using System;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using ImGuizmoNET;

using Ktisis.Services;

namespace Ktisis.Interface.Overlay {
	public static class Selection {
		internal static List<DrawItem> DrawQueue = new();
		internal static bool Selecting = false;

		public static DrawItem AddItem(string name, Vector2 pos, uint color = 0xffffffff) {
			var item = new DrawItem(name, pos, color);
			DrawQueue.Add(item);
			return item;
		}

		public static void Draw() {
			ClickedItem = null;

			if (GuiOverlay.GizmoOwner != null && !Ktisis.Configuration.DrawDotsWithGizmo)
				return;

			var draw = ImGui.GetWindowDrawList();

			var hovered = new List<string>();

			// Draw them dots

			var isManipulating = ImGuizmo.IsUsing();
			var isCursorBusy = false; // TODO GuiOverlay.IsCursorBusy();

			foreach (var dot in DrawQueue) {
				var col = dot.Color;
				if (dot.IsHovered() && !isCursorBusy) {
					col |= 0xff000000;
					hovered.Add(dot.Name);
				}

				var radius = dot.GetRadius();
				if (!isManipulating) {
					draw.AddCircleFilled(dot.Pos, radius, col);
					draw.AddCircle(dot.Pos, radius, 0xaf000000);
				}
			}
			Selecting = hovered.Count > 0;

			// Selection list

			if (Selecting && !isCursorBusy)
				DrawList(hovered);

			// Empty draw queue
			DrawQueue.Clear();
		}

		// Selection list
		// Shows when any item is hovered, allows the user to select a specific item when hovering multiple.

		public static int ScrollIndex = 0;
		public static string? ClickedItem = null;

		public static void DrawList(List<string> items) {
			if (ImGuizmo.IsUsing())
				return;

			// Capture mouse input to intercept mouse clicks.
			// Note that this prevents button presses from going to the game!
			ImGui.SetNextFrameWantCaptureMouse(true);

			var mousePos = ImGui.GetMousePos();
			ImGui.SetNextWindowPos(mousePos + new Vector2(20, 0));
			ImGui.SetNextWindowSize(new Vector2(-1, -1), ImGuiCond.Always);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Bone Selector", ImGuiWindowFlags.NoDecoration)) {
				var mouseDown = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
				var mouseWheel = ImGui.GetIO().MouseWheel;

				ScrollIndex -= (int)mouseWheel;
				if (ScrollIndex >= items.Count)
					ScrollIndex = 0;
				else if (ScrollIndex < 0)
					ScrollIndex = items.Count - 1;

				for (var i = 0; i < items.Count; i++) {
					var name = items[i];
					var isSelected = i == ScrollIndex;
					ImGui.Selectable(name, isSelected);
					if (isSelected && mouseDown)
						ClickedItem = name;
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}

	public class DrawItem {
		public string Name;
		public Vector2 Pos;
		public uint Color;

		public DrawItem(string name, Vector2 pos, uint color = 0xffffffff) {
			Name = name;
			Pos = pos;
			Color = color;
		}

		public unsafe float GetRadius() {
			var dist = DalamudServices.Camera->Camera->InterpDistance;
			return Math.Max(2f, (15f - dist) * (Ktisis.Configuration.SkeletonDotRadius / 7.5f));
		}

		public bool IsHovered() {
			var rad = GetRadius();
			var rect = new Vector2(rad + 4, rad + 4);
			return ImGui.IsMouseHoveringRect(Pos - rect, Pos + rect);
		}

		public bool IsClicked() => Name == Selection.ClickedItem;
	}
}
