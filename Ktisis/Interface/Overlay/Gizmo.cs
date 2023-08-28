using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Overlay;

public class Gizmo {
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
		this.HasDrawn = false;
		this.HasMoved = false;

		this.ViewMatrix = view;
		this.ProjMatrix = proj;

		this.DeltaMatrix = Matrix4x4.Identity;

		this.SetMatrixCallback = null;

		ImGuizmo.Gizmo.BeginFrame();

		var ws = ImGui.GetWindowSize();
		ImGuizmo.Gizmo.SetDrawRect(0, 0, ws.X, ws.Y);
	}

	internal void Manipulate(Matrix4x4 mx, SetMatrixDelegate callback, bool selected) {
		if (this.HasDrawn) {
			if (!this.HasMoved) return;
			callback.Invoke(mx * DeltaMatrix);
		} else if (selected) {
			this.HasDrawn = true;
			this.HasMoved = ImGuizmo.Gizmo.Manipulate(
				this.ViewMatrix,
				this.ProjMatrix,
				Operation.UNIVERSAL,
				Mode.Local,
				ref mx,
				out this.DeltaMatrix
			);

			if (this.HasMoved) {
				callback.Invoke(mx);
				this.SetMatrixCallback?.Invoke(this.DeltaMatrix);
			}
			this.SetMatrixCallback = null;
		} else {
			this.SetMatrixCallback += delta => callback.Invoke(mx * delta);
		}
	}
}
