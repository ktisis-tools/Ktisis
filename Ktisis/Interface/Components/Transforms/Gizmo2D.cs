using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

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
	
	public void SetLookAt(Vector3 cameraPos, Vector3 targetPos, float fov, float aspect = 1.0f) {
		var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
			fieldOfView: fov,
			aspectRatio: 1.0f,
			nearPlaneDistance: 0.1f,
			farPlaneDistance: 100.0f
		);
		
		var viewMatrix = Matrix4x4.CreateLookAt(cameraPos, targetPos, Vector3.UnitY);
		this.Gizmo.SetMatrix(viewMatrix, projectionMatrix);
	}

	public void Begin(Vector2 rectSize) {
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
		
		ImGui.BeginChildFrame(0xD546_0+(uint)this.Gizmo.Id, rectSize);

		var cursorPos = ImGui.GetCursorScreenPos();
		var innerSize = ImGui.GetContentRegionAvail();
		
		ImGui.Begin("##Gizmo2D", ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration);
		
		var minDim = Math.Min(innerSize.X, innerSize.Y);
		var drawSize = new Vector2(minDim, minDim);
		var drawPos = cursorPos + (innerSize - drawSize) / 2;
		
		this.Gizmo.BeginFrame(drawPos, drawSize);
		this.Gizmo.PushDrawList();
		DrawGizmoCircle(drawPos, drawSize, drawSize.X);
	}
	
	private static void DrawGizmoCircle(Vector2 pos, Vector2 size, float width) {
		ImGui.GetWindowDrawList().AddCircleFilled(pos + size / 2, (width * ScaleFactor) / 2.05f, 0xCF202020);
	}

	public bool Manipulate(ref Matrix4x4 matrix, out Matrix4x4 delta)
		=> this.Gizmo.Manipulate(ref matrix, out delta);
	
	public void End() {
		this.Gizmo.EndFrame();
		ImGui.End();
		ImGui.EndChildFrame();
	}
}
