using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes.BaseContainer {
	public class Window : IModularItem, IModularContainer {
		public List<IModularItem> Items { get; }
		public ParamsExtra Extra { get; set; }
		protected int Id;
		public string Title { get; set; }
		public string LocaleHandle { get; set; }
		public ImGuiWindowFlags WindowFlags { get; set; }

		public Window(List<IModularItem> items, ParamsExtra extra) {
			this.Items = items;
			this.Extra = extra;

			string? localeHandle = null;
			Extra.Strings?.TryGetValue("LocaleHandle", out localeHandle);
			this.LocaleHandle = localeHandle ?? "ModularContainer";

			if (Extra.Ints.TryGetValue("Id", out int windowId))
				this.Id = windowId;
			else
				this.Id = 1023;

			if (Extra.Ints.TryGetValue("WindowFlags", out int drawFlags))
				this.WindowFlags = (ImGuiWindowFlags)drawFlags;
			else
				this.WindowFlags = ImGuiWindowFlags.None;

			string? title = null;
			Extra.Strings?.TryGetValue("Title", out title);
			this.Title = title ?? $"Window {this.Id}";
		}
		public Window(List<IModularItem> items, ParamsExtra extra, ImGuiWindowFlags windowFlags) : this(items, extra) {
			this.AddWindowFlags(windowFlags);
		}

		private void AddWindowFlags(ImGuiWindowFlags windowFlags) {
			this.WindowFlags |= windowFlags;
			if (this.Extra.Ints.TryGetValue("WindowFlags", out int _))
				this.Extra.Ints["WindowFlags"] |= (int)this.WindowFlags;
			else
				this.Extra.Ints.Add("WindowFlags", (int)this.WindowFlags);
		}

		virtual public string LocaleName() => $"{Locale.GetString(this.LocaleHandle)}##Modular##Window##{this.Title}##{this.Id}";
		virtual public void Draw() {
			if (ImGui.Begin($"{this.LocaleName()}##ModularWindow##{this.Title}##{this.Id}", this.WindowFlags)) {
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
		public WindowResizable(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }
	}
	public class WindowAutoResize : BaseContainer.Window {
		public WindowAutoResize(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.AlwaysAutoResize) { }
	}
	public class WindowBar : BaseContainer.Window {
		public WindowBar(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize) { }
		protected override void DrawItem(IModularItem item) {
			ImGui.SameLine();
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetStyle().ItemSpacing.X * 2));
			base.DrawItem(item);
		}
	}
	public class TopScreenBar : BaseContainer.Window {
		public TopScreenBar(List<IModularItem> items, ParamsExtra extra) : base(items, extra, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.AlwaysAutoResize) { }
		public override void Draw() {

			ImGui.SetNextWindowPos(new(0));
			ImGui.SetNextWindowSize(new(ImGui.GetIO().DisplaySize.X, -1));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
			if (ImGui.Begin($"{this.LocaleName()}##ModularWindow##{this.Title}##{this.Id}", this.WindowFlags)) {
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