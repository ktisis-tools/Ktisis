using System.Numerics;

using ImGuiNET;
using ImGuizmoNET;

namespace Ktisis.Overlay {
	public class Gizmo {
		// Instanced properties

		public MODE Mode;
		public OPERATION Operation;

		public Matrix4x4 Matrix;
		public Matrix4x4 Delta;

		// Constructor

		public Gizmo(MODE mode = MODE.WORLD, OPERATION op = OPERATION.UNIVERSAL) {
			Mode = mode;
			Operation = op;

			Matrix = new();
			Delta = new();
		}

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
			ImGuizmo.BeginFrame();
			ImGuizmo.SetDrawlist();

			ImGuizmo.SetRect(wp.X, wp.Y, io.DisplaySize.X, io.DisplaySize.Y);

			ImGuizmo.AllowAxisFlip(Ktisis.Configuration.AllowAxisFlip);
		}

		internal unsafe void Manipulate() {
			ImGuizmo.Manipulate(
				ref OverlayWindow.WorldMatrix->Projection.M11,
				ref OverlayWindow.ViewMatrix[0],
				Operation,
				Mode,
				ref Matrix.M11
			);
		}

		public void Draw() {
			Manipulate();
		}

		public void Draw(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ComposeMatrix(ref pos, ref rot, ref scale);
			Manipulate();
			DecomposeMatrix(ref pos, ref rot, ref scale);
		}

		public void Draw(ref Vector3 pos) {
			var _ = new Vector3(0, 0, 0);
			Draw(ref pos, ref _, ref _);
		}
	}
}