using ImGuiNET;

namespace Ktisis.Interface.Modular {
	public class Container {
		public static void Window(ContentsInfo contentsInfo) {
			if (ImGui.Begin(contentsInfo.Handle))
				contentsInfo.Actions?.ForEach(a => a.Item1?.DynamicInvoke(a.Item2));
			ImGui.End();
		}
	}
}
