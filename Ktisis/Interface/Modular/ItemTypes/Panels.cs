using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : IModularItem {
		public void Draw() => Components.ActorsList.Draw();
	}
	public class ControlButtonsExtra : IModularItem {
		public void Draw() => Components.ControlButtons.DrawExtra();
	}
	public class HandleEmpty : IModularItem {
		public void Draw() => ImGui.Text("       ");
	}
	public class GizmoOperations : IModularItem {
		public void Draw() => Components.ControlButtons.DrawGizmoOperations();
	}
	public class GposeTextIndicator : IModularItem {
		public void Draw() => Windows.Workspace.Workspace.DrawGposeIndicator();
	}

}
