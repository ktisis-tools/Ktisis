using ImGuiNET;

namespace Ktisis.Interface.Modular {
	public class Container {
		public static void WindowResizable(ContentsInfo contentsInfo) {
			if (ImGui.Begin(contentsInfo.Handle))
				contentsInfo.Actions?.ForEach(a => a.Item1?.DynamicInvoke(a.Item2));
			ImGui.End();
		}
		public static void WindowAutoResize(ContentsInfo contentsInfo) {
			if (ImGui.Begin(contentsInfo.Handle, ImGuiWindowFlags.AlwaysAutoResize))
				contentsInfo.Actions?.ForEach(a => a.Item1?.DynamicInvoke(a.Item2));
			ImGui.End();
		}
		public static void WindowBar(ContentsInfo contentsInfo) {
			if (ImGui.Begin(contentsInfo.Handle, ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize)) {
				contentsInfo.Actions?.ForEach(a => {
					ImGui.SameLine();
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
					a.Item1?.DynamicInvoke(a.Item2);
				});
			}
			ImGui.End();
		}
	}
}
