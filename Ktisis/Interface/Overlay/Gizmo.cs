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

		ImGuizmo.Gizmo.BeginFrame();

		var ws = ImGui.GetWindowSize();
		ImGuizmo.Gizmo.SetDrawRect(0, 0, ws.X, ws.Y);
	}

	internal bool Manipulate(ref Matrix4x4 mx) {
		if (HasDrawn) {
			if (HasMoved) {
				mx *= DeltaMatrix;
				return true;
			}
			return false;
		}
		
		return Draw(ref mx);
	}

	private bool Draw(ref Matrix4x4 mx) {
		HasDrawn = true;

		return HasMoved = ImGuizmo.Gizmo.Manipulate(
			ViewMatrix,
			ProjMatrix,
			Operation.UNIVERSAL,
			Mode.Local,
			ref mx,
			out DeltaMatrix
		);
	}
}