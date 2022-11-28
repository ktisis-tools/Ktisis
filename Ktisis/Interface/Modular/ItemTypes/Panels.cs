using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : IModularItem {
		public void Draw() => ActorsList.Draw();
	}
	public class ControlButtonsExtra : IModularItem {
		public void Draw() => ControlButtons.DrawExtra();
	}
	public class HandleEmpty : IModularItem {
		public void Draw() => ImGui.Text("       ");
	}
	public class GizmoOperations : IModularItem {
		public void Draw() => ControlButtons.DrawGizmoOperations();
	}
	public class GposeTextIndicator : IModularItem {
		public void Draw() => Workspace.DrawGposeIndicator();
	}
	public class SelectInfo : IModularItem {
		public void Draw() => Workspace.SelectInfo();
	}

}
