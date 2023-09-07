using System;
using System.Numerics;

using ImGuiNET;

using Dalamud.Logging;

using Ktisis.ImGuizmo;
using Ktisis.Common.Utility;

namespace Ktisis.Interface.Overlay;

public enum GizmoID : int {
	Default = -1,
	OverlayMain,
	TransformEditor
}

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

	public static Gizmo? Create(GizmoID gizmoId) {
		if (!Gizmo.IsInit && !Gizmo.InitLibrary())
			return null;
		return new Gizmo(gizmoId);
	}
	
	// Constructor

	public readonly GizmoID Id;

	public Gizmo(GizmoID gizmoId = GizmoID.Default) {
		this.Id = gizmoId;
	}
	
	// Properties
	
	public float ScaleFactor = 0.1f;
	
	// Events

	public event OnManipulateHandler? OnManipulate;
	
	// Gizmo state

	private bool HasDrawn;
	private bool HasMoved;

	private Matrix4x4 ViewMatrix = Matrix4x4.Identity;
	private Matrix4x4 ProjMatrix = Matrix4x4.Identity;

	private Matrix4x4 ResultMatrix = Matrix4x4.Identity;
	private Matrix4x4 DeltaMatrix = Matrix4x4.Identity;

	public Mode Mode = Mode.Local;
	public Operation Operation = Operation.UNIVERSAL;
	
	// Draw

	public void SetMatrix(Matrix4x4 view, Matrix4x4 proj) {
		this.ViewMatrix = view;
		this.ProjMatrix = proj;
	}

	public void BeginFrame(Vector2 pos, Vector2 size) {
		this.HasDrawn = false;
		this.HasMoved = false;
		
		ImGuizmo.Gizmo.SetDrawRect(pos.X, pos.Y, size.X, size.Y);
        
		ImGuizmo.Gizmo.ID = (int)this.Id;
		ImGuizmo.Gizmo.GizmoScale = this.ScaleFactor;
		ImGuizmo.Gizmo.BeginFrame();
	}

	public unsafe void PushDrawList() {
		ImGuizmo.Gizmo.DrawList = (nint)ImGui.GetWindowDrawList().NativePtr;
	}

	public bool ManipulateIm(ref Matrix4x4 mx, out Matrix4x4 delta) {
		delta = Matrix4x4.Identity;
		
		if (this.HasDrawn) return false;
		
		var result = ImGuizmo.Gizmo.Manipulate(
			this.ViewMatrix,
			this.ProjMatrix,
			this.Operation,
			this.Mode,
			ref mx,
			out delta
		);

		this.HasDrawn = true;

		return result;
	}

	public void Manipulate(Matrix4x4 mx) {
		this.HasMoved |= ManipulateIm(ref mx, out this.DeltaMatrix);
		this.ResultMatrix = mx;
	}

	public void EndFrame() {
		if (this.HasMoved)
			this.OnManipulate?.Invoke(this);
	}
	
	// Matrix access

	public Matrix4x4 GetResult() => this.HasMoved ? this.ResultMatrix : Matrix4x4.Identity;

	public Matrix4x4 GetDelta() => this.HasMoved ? this.DeltaMatrix : Matrix4x4.Identity;
}
