using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.ImGuizmo;
using Ktisis.Interface.Overlay;

using Gizmo = Ktisis.Interface.Overlay.Gizmo;

namespace Ktisis.Interface.Components; 

// For drawing gizmos over ImGui windows (ie. Transform Editor)

public class Gizmo2D {
	public const float ScaleFactor = 0.5f;
	
	// Constructor & creation

	private readonly Gizmo Gizmo;

	private Gizmo2D(Gizmo gizmo) {
		this.Gizmo = gizmo;
	}

	public static Gizmo2D? Create(GizmoID id) {
		if (Gizmo.Create(id) is Gizmo gizmo) {
			gizmo.ScaleFactor = ScaleFactor;
			gizmo.Operation = Operation.ROTATE;
			return new Gizmo2D(gizmo);
		}

		return null;
	}
	
	// Gizmo state

	public Mode Mode {
		get => this.Gizmo.Mode;
		set => this.Gizmo.Mode = value;
	}
	
	// Gizmo events

	public event OnDeactivateHandler? OnDeactivate {
		add => this.Gizmo.OnDeactivate += value;
		remove => this.Gizmo.OnDeactivate -= value;
	}

	// Draw

	private readonly static Matrix4x4 _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
		fieldOfView: 0.01745f,
		aspectRatio: 1f, // 1:1
		nearPlaneDistance: 0.1f,
		farPlaneDistance: 100.0f
	);

	public void SetLookAt(Vector3 cameraPos, Vector3 targetPos) {
		var viewMatrix = Matrix4x4.CreateLookAt(cameraPos, targetPos, Vector3.UnitY);
		this.Gizmo.SetMatrix(viewMatrix, _projectionMatrix);
	}
	
	public void SetLookAt(Vector3 cameraPos, Vector3 targetPos, float fov) {
		var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
			fieldOfView: fov,
			aspectRatio: 1f, // 1:1
			nearPlaneDistance: 0.1f,
			farPlaneDistance: 100.0f
		);
		
		var viewMatrix = Matrix4x4.CreateLookAt(cameraPos, targetPos, Vector3.UnitY);
		this.Gizmo.SetMatrix(viewMatrix, projectionMatrix);
	}

	public void Begin(Vector2 rectSize) {
		var rectPos = ImGui.GetCursorScreenPos();
		
		ImGui.BeginChildFrame(0xD546_0+(uint)this.Gizmo.Id, rectSize);
		
		var io = ImGui.GetIO();
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(io.DisplaySize);
		ImGui.Begin("##Gizmo2D", ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoMove);

		this.Gizmo.BeginFrame(rectPos, rectSize);
		this.Gizmo.PushDrawList();
	}

	public bool Manipulate(ref Matrix4x4 matrix, out Matrix4x4 delta)
		=> this.Gizmo.ManipulateIm(ref matrix, out delta);
	
	public void End() {
		this.Gizmo.EndFrame();
		ImGui.End();
		ImGui.EndChildFrame();
	}
}
