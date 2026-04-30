using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

namespace Ktisis.Overlay {
	public static class Selection {
		internal static List<DrawItem> DrawQueue = new();
		internal static bool Selecting = false;

		public static DrawItem AddItem(string name, Vector3 pos, uint color = 0xffffffff, int prio = -1) {
			var item = new DrawItem(name, pos, color, prio);
			DrawQueue.Add(item);
			return item;
		}

		public static void Draw() {
			ClickedItem = null;

			if (OverlayWindow.GizmoOwner != null && !Ktisis.Configuration.DrawDotsWithGizmo)
				return;

			var draw = ImGui.GetWindowDrawList();

			var hovered = new List<DrawItem>();

			// Draw them dots

			var isManipulating = ImGuizmo.IsUsing();
			var isCursorBusy = OverlayWindow.IsCursorBusy();

			foreach (var dot in DrawQueue) {
				var col = dot.Color;
				if (dot.IsHovered() && !isCursorBusy) {
					col |= 0xff000000;
					hovered.Add(dot);
				}

				var radius = dot.GetRadius();
				draw.AddCircleFilled(dot.Pos, radius, col);
				if(!isManipulating) draw.AddCircle(dot.Pos, radius, 0xaf000000);
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
		public static DrawItem? ClickedItem = null;

		public static void DrawList(List<DrawItem> items) {
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

				if (Ktisis.Configuration.OrderBoneListByDistance) {
					items.Sort((x, y) => {
						if (Math.Abs(x.Depth - y.Depth) < 0.01f)
							return x.Prio - y.Prio;
						return x.Depth < y.Depth ? -1 : 1;
					});
				}

				for (var i = 0; i < items.Count; i++) {
					var item = items[i];
					var isSelected = i == ScrollIndex;
					ImGui.Selectable(item.Name, isSelected);
					if (isSelected && mouseDown)
						ClickedItem = item;
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}

	public class DrawItem {
		public readonly string Name;
		public readonly Vector2 Pos;
		public readonly float Depth;
		public readonly uint Color;
		public readonly int Prio;
		
		public DrawItem(string name, Vector3 pos, uint color = 0xffffffff, int prio = 0) {
			Name = name;
			Pos = new Vector2(pos.X, pos.Y);
			Depth = pos.Z;
			Color = color;
			Prio = prio;
		}

		public float GetRadius() {
			return Math.Max(2f, (15f - Depth) * (Ktisis.Configuration.SkeletonDotRadius / 7.5f));
		}

		public bool IsHovered() {
			var rad = GetRadius();
			var rect = new Vector2(rad + 4, rad + 4);
			return ImGui.IsMouseHoveringRect(Pos - rect, Pos + rect);
		}

		public bool IsClicked() => Name == Selection.ClickedItem?.Name;
	}
}
