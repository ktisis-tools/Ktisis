using System.Numerics;

using Matrix = SharpDX.Matrix;

using ImGuiNET;
using ImGuizmoNET;

using Ktisis.Interop;
using Ktisis.Structs.FFXIV;

namespace Ktisis.Overlay {
	public class Gizmo {
		// Static properties

		public unsafe static WorldMatrix* WorldMatrix;

		public float[] ViewMatrix = {
			1.0f, 0.0f, 0.0f, 0.0f,
			0.0f, 1.0f, 0.0f, 0.0f,
			0.0f, 0.0f, 1.0f, 0.0f,
			0.0f, 0.0f, 0.0f, 1.0f
		};

		public static ImGuiIOPtr Io = ImGui.GetIO();
		public static Vector2 Wp = ImGui.GetWindowPos();

		// Instanced properties

		public MODE Mode;
		public OPERATION Operation;

		public Matrix Matrix;
		public Matrix Delta;

		// Constructor

		public Gizmo(MODE mode = MODE.WORLD, OPERATION op = OPERATION.TRANSLATE) {
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

		internal void BeginFrame() {
			ImGuizmo.BeginFrame();
			ImGuizmo.SetDrawlist();
			ImGuizmo.SetRect(Wp.X, Wp.Y, Io.DisplaySize.X, Io.DisplaySize.Y);

			ImGuizmo.AllowAxisFlip(Ktisis.Configuration.AllowAxisFlip);
		}

		internal unsafe void Manipulate() {
			if (WorldMatrix == null)
				WorldMatrix = (WorldMatrix*)CameraHooks.GetMatrix!();

			ImGuizmo.Manipulate(
				ref WorldMatrix->Projection.M11,
				ref ViewMatrix[0],
				Operation,
				Mode,
				ref Matrix.M11,
				ref Delta.M11
			);
		}

		public void Draw() {
			Overlay.Begin();
			BeginFrame();
			Manipulate();
			Overlay.End();
		}

		public void Draw(ref Vector3 pos, ref Vector3 rot, ref Vector3 scale) {
			ComposeMatrix(ref pos, ref rot, ref scale);
			Draw();
			DecomposeMatrix(ref pos, ref rot, ref scale);
		}
	}
}