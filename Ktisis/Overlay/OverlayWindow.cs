using System.Numerics;

using ImGuiNET;

namespace Ktisis.Overlay {
	public class OverlayWindow {
		public static bool HasBegun = false;

		public static Gizmo? Gizmo = null;
		public static string? GizmoOwner = null;

		public static bool IsGizmoOwner(string id) => GizmoOwner == id;
		public static void SetGizmoOwner(string id) => GizmoOwner = id;
		public static Gizmo? GetGizmo(string id) => IsGizmoOwner(id) ? Gizmo : null;

		public static void Clear() {
			Gizmo = null;
			GizmoOwner = null;
		}

		public static void Begin() {
			if (HasBegun) return;

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			HasBegun = true;
		}

		public static void End() {
			if (!HasBegun) return;
			ImGui.End();
			ImGui.PopStyleVar();
			HasBegun = false;
		}

		public static void Draw() {
			if (GizmoOwner != null) {
				Begin();
				if (Gizmo == null)
					Gizmo = new();
				Gizmo.Draw();
			}
			End();
		}
	}
}