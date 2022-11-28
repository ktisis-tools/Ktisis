using System.Collections.Generic;

using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.BaseContainer {
	public class Window : IModularItem, IModularContainer {
		private readonly int windowID;

		public ImGuiWindowFlags DrawFlags { get; }
		public List<IModularItem> Items { get; }
		public string Title { get; set; }

		public Window(int windowID, ImGuiWindowFlags drawFlags, string title) : this(windowID, drawFlags, title, new()) { }

		public Window(int windowID, ImGuiWindowFlags drawFlags, string title, List<IModularItem> items) {
			this.windowID = windowID;
			this.DrawFlags = drawFlags;
			this.Items = items;
			this.Title = title;
		}

		public void Draw() {
			if (ImGui.Begin($"{this.Title}##ModularWindow##{this.windowID}", this.DrawFlags)) {
				if (this.Items != null)
					foreach (var item in this.Items) {
						this.DrawItem(item);
					}
			}
			ImGui.End();
		}

		virtual protected void DrawItem(IModularItem item) {
			item.Draw();
		}

	}
}
namespace Ktisis.Interface.Modular.ItemTypes.Container {

		public class WindowResizable : BaseContainer.Window {
		public WindowResizable(int windowID, string title, List<IModularItem> items) : base(windowID, ImGuiWindowFlags.None, title, items) { }
		public WindowResizable(int windowID, string title) : this(windowID, title, new()) { }
	}
	public class WindowAutoResize : BaseContainer.Window {
		public WindowAutoResize(int windowID, string title, List<IModularItem> items) : base(windowID, ImGuiWindowFlags.AlwaysAutoResize, title, items) { }
		public WindowAutoResize(int windowID, string title) : this(windowID, title, new()) { }
	}
	public class WindowBar : BaseContainer.Window {
		public WindowBar(int windowID, string title, List<IModularItem> items) : base(windowID, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize, title, items) { }
		public WindowBar(int windowID, string title) : this(windowID, title, new()) { }
		protected override void DrawItem(IModularItem item) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			base.DrawItem(item);
		}
	}
}