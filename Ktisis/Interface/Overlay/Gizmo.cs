using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Overlay;

internal class Gizmo {
	// Static

	private const string ImGuiVersion = "1.88";

	private static bool IsInit;
	private unsafe static bool InitLibrary() {
		var success = false;

		try {
			var imVer = ImGui.GetVersion();
			if (imVer != ImGuiVersion)
				throw new Exception($"ImGui version mismatch! Expected {ImGuiVersion}, got {imVer ?? "NULL"} instead.");

			var ctx = ImGui.GetCurrentContext();

			var alloc = nint.Zero;
			var free = nint.Zero;
			var userData = (void*)null;
			ImGui.GetAllocatorFunctions(ref alloc, ref free, ref userData);

			ImGuizmo.Gizmo.Initialize(ctx, alloc, free, (nint)userData);

			success = true;
		} catch (Exception e) {
			PluginLog.Error($"Failed to initialize gizmo:\n{e}");
		}

		PluginLog.Verbose($"Completed gizmo initialization (success: {success}).");

		return IsInit = success;
	}

	internal static Gizmo? Create() {
		if (!Gizmo.IsInit && !Gizmo.InitLibrary())
			return null;
		return new Gizmo();
	}

	// Callback handling

	internal delegate void SetMatrixDelegate(Matrix4x4 mx);

	private SetMatrixDelegate? SetMatrixCallback;

	// Gizmo state

	private bool HasDrawn;
	private bool HasMoved;

	private Matrix4x4 ViewMatrix = Matrix4x4.Identity;
	private Matrix4x4 ProjMatrix = Matrix4x4.Identity;
	private Matrix4x4 DeltaMatrix = Matrix4x4.Identity;

	internal void BeginFrame(Matrix4x4 view, Matrix4x4 proj) {
		HasDrawn = false;
		HasMoved = false;

		ViewMatrix = view;
		ProjMatrix = proj;

		DeltaMatrix = Matrix4x4.Identity;

		SetMatrixCallback = null;

		ImGuizmo.Gizmo.BeginFrame();

		var ws = ImGui.GetWindowSize();
		ImGuizmo.Gizmo.SetDrawRect(0, 0, ws.X, ws.Y);
	}

	internal void Manipulate(Matrix4x4 mx, SetMatrixDelegate callback, bool selected) {
		if (HasDrawn) {
			if (!HasMoved) return;
			callback.Invoke(mx * DeltaMatrix);
		} else if (selected) {
			HasDrawn = true;
			HasMoved = ImGuizmo.Gizmo.Manipulate(
				ViewMatrix,
				ProjMatrix,
				Operation.UNIVERSAL,
				Mode.Local,
				ref mx,
				out DeltaMatrix
			);

			if (HasMoved) {
				callback.Invoke(mx);
				SetMatrixCallback?.Invoke(DeltaMatrix);
			}
			SetMatrixCallback = null;
		} else {
			SetMatrixCallback += delta => callback.Invoke(mx * delta);
		}
	}
}
