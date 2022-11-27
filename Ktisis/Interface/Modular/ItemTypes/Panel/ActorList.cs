using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.Panel {
	public class ActorList : IModularItem {
		public void Draw() {
			Components.ActorsList.Draw();
		}
	}
	public class HandleEmpty : IModularItem {
		public void Draw() {
			ImGui.Text("       ");
		}
	}
}
