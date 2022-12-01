using System.Collections.Generic;

using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.Container {

	public class WindowResizable : BaseContainer {
		public WindowResizable(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }
	}
	public class WindowAutoResize : BaseContainer {
		public WindowAutoResize(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.AlwaysAutoResize) { }
	}
	public class WindowBar : BaseContainer {
		public WindowBar(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize) { }
		protected override void DrawItem(IModularItem item) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			base.DrawItem(item);
		}
	}
	public class TopScreenBar : BaseContainer {
		public TopScreenBar(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize) { }
		public override void Draw() {

			ImGui.SetNextWindowPos(new(0));
			ImGui.SetNextWindowSize(new(ImGui.GetIO().DisplaySize.X, -1));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			if (ImGui.Begin(this.GetTitle(), this.WindowFlags)) {
				if (this.Items != null)
					foreach (var item in this.Items) {
						this.DrawItem(item);
					}
			}
			ImGui.End();
			ImGui.PopStyleVar(2);
		}
		protected override void DrawItem(IModularItem item) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			base.DrawItem(item);
		}
	}
}