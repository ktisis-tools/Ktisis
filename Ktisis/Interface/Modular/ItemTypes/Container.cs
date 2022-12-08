using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.Container {

	public class Window : BaseContainer {
		public ImGuiWindowFlags WindowFlags { get; set; }
		public Window() : base() { }

		private static readonly List<ImGuiWindowFlags> AllowedWindowFlags = new() {
			ImGuiWindowFlags.None,
			ImGuiWindowFlags.NoTitleBar,
			ImGuiWindowFlags.NoResize,
			ImGuiWindowFlags.NoMove,
			ImGuiWindowFlags.NoScrollbar,
			ImGuiWindowFlags.NoCollapse,
			ImGuiWindowFlags.NoDecoration,
			ImGuiWindowFlags.AlwaysAutoResize,
			ImGuiWindowFlags.NoBackground,
			ImGuiWindowFlags.NoSavedSettings,
			ImGuiWindowFlags.NoMouseInputs,
			ImGuiWindowFlags.HorizontalScrollbar,
			ImGuiWindowFlags.NoFocusOnAppearing,
			ImGuiWindowFlags.NoBringToFrontOnFocus,
			ImGuiWindowFlags.AlwaysVerticalScrollbar,
			ImGuiWindowFlags.AlwaysHorizontalScrollbar,
			ImGuiWindowFlags.AlwaysUseWindowPadding,
		};

		public Window(ImGuiWindowFlags windowFlags) : this() {
			this.WindowFlags = windowFlags;
		}
		override public void Draw() {
			if (ImGui.Begin(this.GetTitle(), this.WindowFlags)) {
				if (this.Items != null)
					foreach (var item in this.Items) {
						this.DrawItem(item);
					}
			}
			ImGui.End();
		}
		public override void DrawConfig() {
			base.DrawConfig();
			var windowFlags = WindowFlags;
			Configurator.DrawBitwiseFlagSelect("Window Flags", ref windowFlags, AllowedWindowFlags);
			WindowFlags = windowFlags;
		}
	}


	public class ResizableWindow : Window { }
	public class WindowAutoResize : Window {
		public WindowAutoResize() : base(ImGuiWindowFlags.AlwaysAutoResize) { }
	}
	public class BarWindow : Window {
		public BarWindow() : base(ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize) { }
		protected override void DrawItem(IModularItem item) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			base.DrawItem(item);
		}
	}
	public class BorderWindow : Window {
		enum WindowLocation { Top, Bottom, Left, Right }
		WindowLocation Location { get; set; } = WindowLocation.Top;
		float ReverseOffset = -100;
		public BorderWindow() : base(ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize) { }
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
		public override void DrawConfig() {
			base.DrawConfig();
			var location = Location;
			Configurator.DrawFlagSelect("Location", ref location);
			Location = location;
		}

	}
}