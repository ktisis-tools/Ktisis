using System.Numerics;

using ImGuiNET;

using Ktisis.Interop;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Overlay {
	public class OverlayWindow {
		// Rendering

		public unsafe static WorldMatrix* WorldMatrix;

		public static float[] ViewMatrix = {
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};

		// Gizmo

		public static bool HasBegun = false;

		public static Gizmo Gizmo = new();
		public static string? GizmoOwner = null;

		public static bool IsGizmoVisible => Gizmo != null && GizmoOwner != null;

		public static bool IsGizmoOwner(string? id) => GizmoOwner == id;
		public static Gizmo? SetGizmoOwner(string? id) {
			GizmoOwner = id;
			return id == null ? null : Gizmo;
		}
		public static Gizmo? GetGizmo(string? id) => IsGizmoOwner(id) ? Gizmo : null;

		public static void Begin() {
			if (HasBegun) return;

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);

			var io = ImGui.GetIO();
			ImGui.SetWindowSize(io.DisplaySize);

			var wp = ImGui.GetWindowPos();
			Gizmo.BeginFrame(wp, io);

			HasBegun = true;
		}

		public static void End() {
			if (!HasBegun) return;
			ImGui.End();
			ImGui.PopStyleVar();
			HasBegun = false;
		}

		public unsafe static void Draw() {
			if (WorldMatrix == null)
				WorldMatrix = (WorldMatrix*)CameraHooks.GetMatrix!();

			if (IsGizmoVisible)
				Begin();

			End();
		}
	}
}