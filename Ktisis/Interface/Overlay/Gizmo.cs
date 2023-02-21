using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.ImGuizmo;

using Ktisis.Services;
using Ktisis.Library.Extensions;

namespace Ktisis.Interface.Overlay {
	public class Gizmo {
		// Instanced properties

		public Mode Mode => Ktisis.Configuration.GizmoMode;
		public Operation Operation => Ktisis.Configuration.GizmoOp;

		public Matrix4x4 Matrix = new();

		public SharpDX.Matrix3x3 EulerDeltaMatrix = new(); // for non gizmo manipulation, euler based, which must have an effect on the gizmo

		public Operation? ForceOp = null;

		// Compose & Decompose
		// Compose updates the matrix using given values.
		// Decompose retrieves values from the matrix.

		public void ComposeMatrix(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			Matrix = ImGuizmo.ImGuizmo.RecomposeMatrixFromComponents(
				pos,
				rot,
				scale
			);
		}

		public void ComposeMatrix(Vector3 pos, Vector3 rot, Vector3 scale)
			=> ComposeMatrix(ref pos, ref rot, ref scale);

		public void DecomposeMatrix(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ImGuizmo.ImGuizmo.DecomposeMatrixToComponents(
				Matrix,
				out pos,
				out rot,
				out scale
			);
		}

		// Draw

		internal void BeginFrame(Vector2 wp, ImGuiIOPtr io) {
			ForceOp = null;

			ImGuizmo.ImGuizmo.BeginFrame();
			ImGuizmo.ImGuizmo.DrawList = IntPtr.Zero;

			ImGuizmo.ImGuizmo.SetDrawRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);

			ImGuizmo.ImGuizmo.AllowAxisFlip = Ktisis.Configuration.AllowAxisFlip;
		}

		public void InsertEulerDeltaMatrix(Vector3 posDelta, Vector3 rotDelta, Vector3 scaDelta) {
			EulerDeltaMatrix = new(
				posDelta.X, posDelta.Y, posDelta.Z,
				rotDelta.X, rotDelta.Y, rotDelta.Z,
				scaDelta.X, scaDelta.Y, scaDelta.Z
			);
		}

		internal bool ManipulateEuler() {
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
			var camera = DalamudServices.Camera->Camera;
			var view = camera->GetViewMatrix();
			var proj = camera->GetProjectionMatrix();

			return ImGuizmo.ImGuizmo.Manipulate(
				view,
				proj,
				ForceOp ?? Operation,
				Mode,
				ref Matrix
			);
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
