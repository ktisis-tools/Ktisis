using System.Numerics;

using Dalamud.Bindings.ImGui;

using Ktisis.Data.Config.Sections;
using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Overlay;

public enum GizmoId : int {
	Default = -1,
	OverlayMain,
	TransformEditor,
	GazeTarget
}

public interface IGizmo {
	public GizmoId Id { get; }
	public bool IsUsedPrev { get; }
	
	public float ScaleFactor { get; set; }
	
	public Mode Mode { get; set; }
	public Operation Operation { get; set; }

	public bool AllowAxisFlip { get; set; }

	public bool IsEnded { get; }

	public void SetMatrix(Matrix4x4 view, Matrix4x4 proj);

	public void BeginFrame(Vector2 pos, Vector2 size);
	public void PushDrawList();

	public bool Manipulate(ref Matrix4x4 mx, out Matrix4x4 delta);

	public void EndFrame();
	public void Reset();
}

public class Gizmo : IGizmo {
	private readonly GizmoConfig _cfg;
	
	public GizmoId Id { get; }
	
	public Gizmo(
		GizmoConfig cfg,
		GizmoId id
	) {
		this._cfg = cfg;
		this.Id = id;
	}
	
	// Proeprties

	public float ScaleFactor { get; set; } = 0.1f;
	
	// State

	public bool IsUsedPrev { get; private set; }
	private bool HasDrawn;

	private Matrix4x4 ViewMatrix = Matrix4x4.Identity;
	private Matrix4x4 ProjMatrix = Matrix4x4.Identity;

	public Mode Mode { get; set; } = Mode.Local;
	public Operation Operation { get; set; } = Operation.UNIVERSAL;

	public bool AllowAxisFlip { get; set; } = true;

	public bool IsEnded { get; private set; }
	
	// Draw

	public void SetMatrix(Matrix4x4 view, Matrix4x4 proj) {
		this.ViewMatrix = view;
		this.ProjMatrix = proj;
	}

	public void BeginFrame(Vector2 pos, Vector2 size) {
		this.HasDrawn = false;

		ImGuizmo.Gizmo.SetDrawRect(pos.X, pos.Y, size.X, size.Y);

		ImGuizmo.Gizmo.ID = (int)this.Id;
		ImGuizmo.Gizmo.GizmoScale = this.ScaleFactor;
		ImGuizmo.Gizmo.AllowAxisFlip = this.AllowAxisFlip;
		ImGuizmo.Gizmo.Style = this._cfg.Style;
		ImGuizmo.Gizmo.BeginFrame();

		this.IsUsedPrev = ImGuizmo.Gizmo.IsUsing;
	}

	public unsafe void PushDrawList() {
		ImGuizmo.Gizmo.DrawList = (nint)ImGui.GetWindowDrawList().Handle;
	}

	public bool Manipulate(ref Matrix4x4 mx, out Matrix4x4 delta) {
		delta = Matrix4x4.Identity;

		if (this.HasDrawn) return false;

		var result = false;
		if (this._cfg.AllowHoldSnap && ImGui.IsKeyDown(ImGuiKey.ModCtrl)) {
			var snap = Vector3.One;
			if (this.Operation is Operation.ROTATE) snap *= 5;
			else snap /= 10;

			if (ImGui.IsKeyDown(ImGuiKey.ModShift))
				snap /= 10;

			result = ImGuizmo.Gizmo.Manipulate(
				this.ViewMatrix,
				this.ProjMatrix,
				this.Operation,
				this.Mode,
				ref mx,
				out delta,
				snap
			);
		} else {
			result = ImGuizmo.Gizmo.Manipulate(
				this.ViewMatrix,
				this.ProjMatrix,
				this.Operation,
				this.Mode,
				ref mx,
				out delta
			);
		}

		this.HasDrawn = true;
		return result;
	}

	public void EndFrame() {
		this.IsEnded = !ImGuizmo.Gizmo.IsUsing && this.IsUsedPrev;
	}

	public void Reset() {
		ImGuizmo.Gizmo.Enable = false;
		ImGuizmo.Gizmo.Enable = true;
		this.IsEnded = true;
	}
}
