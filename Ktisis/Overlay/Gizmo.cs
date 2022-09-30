using ImGuizmoNET;

namespace Ktisis.Overlay {
	public class Gizmo {
		public MODE Mode;
		public OPERATION Operation;

		public Gizmo(MODE mode = MODE.WORLD, OPERATION op = OPERATION.TRANSLATE) {
			Mode = mode;
			Operation = op;
		}
	}
}