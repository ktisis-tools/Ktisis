using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.ImGuizmo;
using Ktisis.Common.Utility;

namespace Ktisis.Interface.Overlay;

public delegate void OnManipulateHandler(Gizmo sender);

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

	public event OnManipulateHandler? OnManipulate;
	
	// Gizmo state

	private bool HasDrawn;
	private bool HasMoved;

	private Matrix4x4 ViewMatrix = Matrix4x4.Identity;
	private Matrix4x4 ProjMatrix = Matrix4x4.Identity;

	private Matrix4x4 ResultMatrix = Matrix4x4.Identity;
	private Matrix4x4 DeltaMatrix = Matrix4x4.Identity;
	
	// Draw

	internal void BeginFrame(Matrix4x4 view, Matrix4x4 proj) {
		this.HasDrawn = false;
		this.HasMoved = false;

		this.ViewMatrix = view;
		this.ProjMatrix = proj;

		ImGuizmo.Gizmo.BeginFrame();

		var ws = ImGui.GetWindowSize();
		ImGuizmo.Gizmo.SetDrawRect(0, 0, ws.X, ws.Y);
	}

	internal void Manipulate(Matrix4x4 mx) {
		if (this.HasDrawn) return;
		
		this.HasMoved = ImGuizmo.Gizmo.Manipulate(
			this.ViewMatrix,
			this.ProjMatrix,
			Operation.UNIVERSAL,
			Mode.Local,
			ref mx,
			out this.DeltaMatrix
		);

		this.ResultMatrix = mx;
		this.HasDrawn = true;
	}

	internal void EndFrame() {
		if (this.HasMoved)
			this.OnManipulate?.Invoke(this);
	}
	
	// Matrix access

	public Matrix4x4 GetResult() => this.HasMoved ? this.ResultMatrix : Matrix4x4.Identity;

	public Matrix4x4 GetDelta() => this.HasMoved ? this.DeltaMatrix : Matrix4x4.Identity;

	public Matrix4x4 ApplyDelta(Matrix4x4 target, Matrix4x4 delta, Matrix4x4? result = null) {
        var deltaT = Transform.FromMatrix(delta);
        result ??= GetResult();
		
		return Matrix4x4.Multiply(
			Matrix4x4.Transform(target, deltaT.Rotation),
			Matrix4x4.CreateTranslation(deltaT.Position) * Matrix4x4.CreateScale(
				deltaT.Scale,
				result.Value.Translation
			)
		);
	}
}