using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;
using Ktisis.ImGuizmo;

using Dalamud.Logging;
using Dalamud.Interface;

using Ktisis.Scene;
using Ktisis.Interop;
using Ktisis.Services;
using Ktisis.Scene.Skeletons;
using Ktisis.Scene.Interfaces;
using Lumina.Models.Models;

namespace Ktisis.Interface.Overlay {
	public static class GuiOverlay {
		public static bool HasBegun = false;

		public static Gizmo Gizmo = new();
		public static string? GizmoOwner = null;

		// Gizmo

		public static Gizmo? GetGizmo(string? id) => IsGizmoOwner(id) ? Gizmo : null;

		public static bool IsGizmoOwner(string? id) => GizmoOwner == id;
		public static Gizmo? SetGizmoOwner(string? id) {
			GizmoOwner = id;
			return id == null ? null : Gizmo;
		}

		public static void DeselectGizmo() {
			SetGizmoOwner(null);
			//Skeleton.BoneSelect.Active = false;

			// This is a hack to reset ImGuizmo's mbUsing state.
			ImGuizmo.ImGuizmo.Enable = false;
			ImGuizmo.ImGuizmo.Enable = true;
		}

		// Begin/End

		private static void Begin() {
			if (HasBegun) return;

			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);

			var io = ImGui.GetIO();
			ImGui.SetWindowSize(io.DisplaySize);

			var wp = ImGui.GetWindowPos();
			Gizmo.BeginFrame(wp, io);

			HasBegun = true;
		}

		private static void End() {
			if (!HasBegun) return;
			ImGui.End();
			ImGui.PopStyleVar();
			HasBegun = false;
		}

		// Draw

		public unsafe static void Draw() {
			if (!GPoseService.IsInGPose || !Ktisis.Configuration.ShowOverlay)
				return;

			var matrix = Methods.GetMatrix();
			if (matrix == null) return;

			Begin();

			DrawItems(EditorService.Items);

			if (Selection.DrawQueue.Count > 0)
				Selection.Draw();

			DrawGizmo();

			End();
		}

		private static void DrawItems(List<Manipulable> items) {
			foreach (var item in items) {
				if (item is SkeletonObject skele) {
					Skeleton.Draw(skele);
				} else continue;

				DrawItems(item.Children);
			}
		}

		private unsafe static void DrawGizmo() {
			var manip = EditorService.Selections.FirstOrDefault(i => i is IGizmoTransform);
			if (manip == null) return;

			var transform = (IGizmoTransform)manip;

			var matrix = transform.GetMatrix();
			if (matrix == null) return;

			var gizmo = SetGizmoOwner(manip.Name)!;

			gizmo.Matrix = (Matrix4x4)matrix;
			if (gizmo.Draw())
				transform.SetMatrix(gizmo.Matrix);
		}
	}
}