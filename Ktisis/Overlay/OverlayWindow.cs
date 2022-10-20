using System;
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

		public static ImGuiIOPtr Io;
		public static Vector2 Wp;

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

			Io = ImGui.GetIO();
			ImGui.SetWindowSize(Io.DisplaySize);

			Wp = ImGui.GetWindowPos();
			Gizmo.BeginFrame(Wp, Io);

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

			var drawSkele = Ktisis.Configuration.ShowSkeleton;

			if (IsGizmoVisible || drawSkele)
				Begin();

			if (drawSkele)
				Skeleton.Draw();

			End();
		}
	}
}