using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes.BaseContainer {
	public class Window : IModularItem, IModularContainer {
		protected readonly int windowID;

		public ImGuiWindowFlags DrawFlags { get; }
		public List<IModularItem> Items { get; }
		public string Title { get; set; }
		public string LocaleHandle { get; set; }

		public Window(int windowID, ImGuiWindowFlags drawFlags, string title) : this(windowID, drawFlags, title, new()) { }

		public Window(int windowID, ImGuiWindowFlags drawFlags, string title, List<IModularItem> items) {
			this.windowID = windowID;
			this.DrawFlags = drawFlags;
			this.Items = items;
			this.Title = title;
			this.LocaleHandle = "ModularContainer";
		}

		virtual public string LocaleName() => $"{Locale.GetString(LocaleHandle)} ({windowID})##Modular##Window##{windowID}";
		virtual public void Draw() {
			if (ImGui.Begin($"{this.LocaleName()}##ModularWindow##{this.Title}##{this.windowID}", this.DrawFlags)) {
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
	public class TopScreenBar : BaseContainer.Window {
		public TopScreenBar(int windowID, string title, List<IModularItem> items) : base(windowID, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize, title, items) { }
		public TopScreenBar(int windowID, string title) : this(windowID, title, new()) { }
		public override void Draw() {

			ImGui.SetNextWindowPos(new(0));
			ImGui.SetNextWindowSize(new(ImGui.GetIO().DisplaySize.X, -1));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			if (ImGui.Begin($"{this.LocaleName()}##ModularWindow##{this.Title}##{this.windowID}", this.DrawFlags)) {
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