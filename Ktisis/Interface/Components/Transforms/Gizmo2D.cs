using System.Numerics;

using Dalamud.Bindings.ImGui;

using Ktisis.ImGuizmo;
using Ktisis.Interface.Overlay;

namespace Ktisis.Interface.Components.Transforms;

public class Gizmo2D {
	public const float ScaleFactor = 0.5f;
	
	// Constructor & creation

	private readonly IGizmo Gizmo;

	public Gizmo2D(IGizmo gizmo) {
		this.Gizmo = gizmo;
		this.Gizmo.Operation = Operation.ROTATE;
		this.Gizmo.ScaleFactor = ScaleFactor;
	}
	
	// Gizmo state

	public Mode Mode {
		get => this.Gizmo.Mode;
		set => this.Gizmo.Mode = value;
	}

	public bool IsEnded => this.Gizmo.IsEnded;

	// Draw

	private static readonly Matrix4x4 ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
		fieldOfView: 0.01745f,
		aspectRatio: 1f, // 1:1
		nearPlaneDistance: 0.1f,
		farPlaneDistance: 100.0f
	);

	public void SetLookAt(Vector3 cameraPos, Vector3 targetPos) {
		var viewMatrix = Matrix4x4.CreateLookAt(cameraPos, targetPos, Vector3.UnitY);
		this.Gizmo.SetMatrix(viewMatrix, ProjectionMatrix);
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
		=> this.Gizmo.Manipulate(ref matrix, out delta);
	
	public void End() {
		this.Gizmo.EndFrame();
		ImGui.End();
		ImGui.EndChildFrame();
	}
}
