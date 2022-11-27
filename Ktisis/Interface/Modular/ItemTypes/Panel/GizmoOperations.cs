namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class GizmoOperations : IModularItem {
		public void Draw() {
			Components.ControlButtons.DrawGizmoOperations();
		}
	}
}
