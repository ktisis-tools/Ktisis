using System.Collections.Generic;
using System.Numerics;

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
		enum WindowLocation { Top, Bottom, Left, Right }
		WindowLocation Location { get; set; } = WindowLocation.Top;
		float ReverseOffset = -100;
		public TopScreenBar(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize) {
			if (this.Extra.Ints.TryGetValue("Location", out int location))
				this.Extra.Ints["Location"] = location;
			else
				this.Extra.Ints.Add("Location", location);
			this.Location = (WindowLocation)location;

		}
		public override void Draw() {

			switch (Location) {
				case WindowLocation.Top:
					ImGui.SetNextWindowPos(Vector2.Zero);
					ImGui.SetNextWindowSize(new(ImGui.GetIO().DisplaySize.X, -1));
					break;
				case WindowLocation.Bottom:
					ImGui.SetNextWindowPos(new(0, ImGui.GetIO().DisplaySize.Y - ReverseOffset));
					ImGui.SetNextWindowSize(new(ImGui.GetIO().DisplaySize.X, -1));
					break;
				case WindowLocation.Left:
					ImGui.SetNextWindowPos(new(0));
					ImGui.SetNextWindowSize(new(-1, ImGui.GetIO().DisplaySize.Y));
					break;
				case WindowLocation.Right:
					ImGui.SetNextWindowPos(new(ImGui.GetIO().DisplaySize.X - ReverseOffset, 0));
					ImGui.SetNextWindowSize(new(-1, ImGui.GetIO().DisplaySize.Y));
					break;
				default:
					break;
			}
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			if (ImGui.Begin(this.GetTitle(), this.WindowFlags)) {
				if(Location == WindowLocation.Bottom) ReverseOffset = ImGui.GetWindowSize().Y;
				if(Location == WindowLocation.Right) ReverseOffset = ImGui.GetWindowSize().X;
				if (this.Items != null)
					foreach (var item in this.Items) {
						this.DrawItem(item);
					}
			}
			ImGui.End();
			ImGui.PopStyleVar(2);
		}
		protected override void DrawItem(IModularItem item) {
			if (Location == WindowLocation.Bottom || Location == WindowLocation.Top) {
				ImGui.SameLine();
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			}
			base.DrawItem(item);
		}
	}
}