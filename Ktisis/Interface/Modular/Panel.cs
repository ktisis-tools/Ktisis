using ImGuiNET;

namespace Ktisis.Interface.Modular {
	internal class Panel {

		public static void ActorsList(ContentsInfo contentsInfo) => Components.ActorsList.Draw();
		public static void GizmoOperations(ContentsInfo contentsInfo) => Components.ControlButtons.DrawGizmoOperations();
		public static void ControlButtonsExtra(ContentsInfo contentsInfo) => Components.ControlButtons.DrawExtra();
		public static void HandleEmpty(ContentsInfo contentsInfo) => ImGui.Text("       ");

	}
}
