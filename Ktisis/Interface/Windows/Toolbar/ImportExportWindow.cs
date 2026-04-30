using System.Numerics;

using Dalamud.Bindings.ImGui;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class ImportExportWindow {
		private static bool Visible = false;
		public static void Toggle() => Visible = !Visible;

		public unsafe static void Draw() {
			if (!Visible || !Ktisis.IsInGPose)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * 16, 1), new Vector2(50000, 50000));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			var target = Ktisis.GPoseTarget;
			if (target == null) return;
			var actor = (Actor*)target.Address;
			if (actor->Model == null) return;
			
			if (ImGui.Begin("Import / Export", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				if(ImGui.CollapsingHeader("Actor", ImGuiTreeNodeFlags.DefaultOpen))
					Workspace.Tabs.ActorTab.ImportExportChara(actor);
				if(ImGui.CollapsingHeader("Pose", ImGuiTreeNodeFlags.DefaultOpen))
					Workspace.Tabs.PoseTab.ImportExportPose(actor);
				ImGui.End();
			}
		}
	}
}
