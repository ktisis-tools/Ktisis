using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

using Ktisis.Structs.Extensions;
using Ktisis.Events;

namespace Ktisis.Overlay {
	public class Gizmo {
		// Instanced properties

		public MODE Mode => Ktisis.Configuration.GizmoMode;
		public OPERATION Operation => Ktisis.Configuration.GizmoOp;

		public Matrix4x4 Matrix = new();
		public Matrix4x4 Delta = new();
		public SharpDX.Matrix3x3 EulerDeltaMatrix = new(); // for non gizmo manipulation, euler based, which must have an effect on the gizmo

		public OPERATION? ForceOp = null;

		private GizmoState _state = GizmoState.IDLE;

		// Compose & Decompose
		// Compose updates the matrix using given values.
		// Decompose retrieves values from the matrix.

		public void ComposeMatrix(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ImGuizmo.RecomposeMatrixFromComponents(
				ref pos.X,
				ref rot.X,
				ref scale.X,
				ref Matrix.M11
			);
		}

		public void ComposeMatrix(Vector3 pos, Vector3 rot, Vector3 scale)
			=> ComposeMatrix(ref pos, ref rot, ref scale);

		public void DecomposeMatrix(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ImGuizmo.DecomposeMatrixToComponents(
				ref Matrix.M11,
				ref pos.X,
				ref rot.X,
				ref scale.X
			);
		}
		public (Vector3, Vector3, Vector3) Decompose()
		{
			Vector3 pos = new();
			Vector3 rot = new();
			Vector3 scale = new();
			DecomposeMatrix(ref pos, ref rot, ref scale);
			return (pos, rot, scale);
		}

		public void DecomposeDelta(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ImGuizmo.DecomposeMatrixToComponents(
				ref Delta.M11,
				ref pos.X,
				ref rot.X,
				ref scale.X
			);
		}

		// Draw

		internal void BeginFrame(Vector2 wp, ImGuiIOPtr io) {
			ForceOp = null;

			ImGuizmo.BeginFrame();
			ImGuizmo.SetDrawlist();

			ImGuizmo.SetRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);

			ImGuizmo.AllowAxisFlip(Ktisis.Configuration.AllowAxisFlip);
		}

		public void InsertEulerDeltaMatrix(Vector3 posDelta,Vector3 rotDelta,Vector3 scaDelta)
		{
			EulerDeltaMatrix = new(
				posDelta.X, posDelta.Y, posDelta.Z,
				rotDelta.X, rotDelta.Y, rotDelta.Z,
				scaDelta.X, scaDelta.Y, scaDelta.Z
			);
		}
		internal bool ManipulateEuler()
		{
			// skip if no delta detected
			bool isActive = EulerDeltaMatrix != new SharpDX.Matrix3x3();
			if (!isActive) return false;

			Vector3 posDelta = new(EulerDeltaMatrix.M11, EulerDeltaMatrix.M12, EulerDeltaMatrix.M13);
			Vector3 rotDelta = new(EulerDeltaMatrix.M21, EulerDeltaMatrix.M22, EulerDeltaMatrix.M23);
			Vector3 scaDelta = new(EulerDeltaMatrix.M31, EulerDeltaMatrix.M32, EulerDeltaMatrix.M33);

			// calculate final euler coordinates from delta
			(Vector3 position, Vector3 rotation, Vector3 scale) = (new(), new(), new());
			DecomposeMatrix(ref position, ref rotation, ref scale);
			position += posDelta;
			rotation += rotDelta;
			scale += scaDelta;

			// apply euler to matrix
			ComposeMatrix(position, rotation, scale);

			EulerDeltaMatrix = new();
			return true;
		}
		internal unsafe bool Manipulate() {
			var camera = Services.Camera->Camera;
			var view = camera->GetViewMatrix();
			var proj = camera->GetProjectionMatrix();

			return ImGuizmo.Manipulate(
				ref view.M11,
				ref proj.M11,
				ForceOp ?? Operation,
				Mode,
				ref Matrix.M11,
				ref Delta.M11
			);
		}

		public void UpdateGizmoState() {
			if ((_state == GizmoState.IDLE) && ImGuizmo.IsUsing())
			{
				_state = GizmoState.EDITING;
			}

			if ((_state == GizmoState.EDITING) && !ImGuizmo.IsUsing())
			{
				_state = GizmoState.IDLE;
			}

			EventManager.FireOnGizmoChangeEvent(_state);
		}

		public bool Draw() => Manipulate();

		public bool Draw(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ComposeMatrix(ref pos, ref rot, ref scale);
			var result = Manipulate();
			DecomposeMatrix(ref pos, ref rot, ref scale);
			return result;
		}

		public bool Draw(ref Vector3 pos) {
			var _ = new Vector3(0, 0, 0);
			return Draw(ref pos, ref _, ref _);
		}
	}
}
