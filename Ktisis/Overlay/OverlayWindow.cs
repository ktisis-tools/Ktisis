using System;
using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Dalamud.Interface;

using Ktisis.Interop;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Overlay {
	public static class OverlayWindow {
		// Rendering

		public unsafe static WorldMatrix* WorldMatrix;

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

		public static void DeselectGizmo() {
			SetGizmoOwner(null);
			Skeleton.BoneSelect.Active = false;

			// This is a hack to reset ImGuizmo's mbUsing state.
			ImGuizmo.Enable(false);
			ImGuizmo.Enable(true);
		}

		public static bool IsCursorBusy() =>
			(GizmoOwner != null && (ImGuizmo.IsUsing() || ImGuizmo.IsOver()))
			|| ImGui.IsAnyItemActive() || ImGui.IsAnyItemHovered()
			|| ImGui.IsAnyItemFocused() || ImGui.IsAnyMouseDown();

		public static void Begin() {
			if (HasBegun) return;

			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);

			Io = ImGui.GetIO();
			ImGui.SetWindowSize(Io.DisplaySize);

			Wp = ImGui.GetWindowPos();
			Gizmo.BeginFrame(Wp, Io);

			HasBegun = true;

			if (Selection.DrawQueue.Count >= 1000) // something *probably* fucked up (thrown error in Selection.Draw?)
				Selection.DrawQueue.Clear(); // don't let it get worse
		}

		public static void End() {
			if (!HasBegun) return;
			ImGui.End();
			ImGui.PopStyleVar();
			HasBegun = false;
		}

		public unsafe static void Draw() {
			if (WorldMatrix == null)
				WorldMatrix = Methods.GetMatrix!();

			// Might need a different name for Begin?

			if (IsGizmoVisible) {
				Gizmo.UpdateGizmoState();
				Begin();
			}

			Skeleton.BoneSelect.Active = false;
			Skeleton.BoneSelect.Update = false;
			if (Ktisis.Configuration.ShowSkeleton) {
				Begin();
				Skeleton.Draw();
			}

			if (Selection.DrawQueue.Count > 0) {
				Begin();
				Selection.Draw();
			}

			End();
		}
	}
}
