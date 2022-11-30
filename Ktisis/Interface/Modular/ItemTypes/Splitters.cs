using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes {

	public class BaseSplitter : IModularItem, IModularContainer {
		protected readonly int windowID;
		public List<IModularItem> Items { get; }
		public string Title { get; set; }
		public string LocaleHandle { get; set; }

		protected BaseSplitter(List<IModularItem> items) : this(1120, "Window 1120", items) { }
		protected BaseSplitter(int windowID, string title) : this(windowID, title, new()) { }

		protected BaseSplitter(int windowID, string title, List<IModularItem> items) {
			this.windowID = windowID;
			this.Items = items;
			this.Title = title;
			this.LocaleHandle = "modularSplitter";
		}

		virtual public string LocaleName() => $"{Locale.GetString(LocaleHandle)}##Modular##Splitter##{windowID}";
		virtual public void Draw() {
			if (this.Items != null)
				foreach (var item in this.Items) {
					this.DrawItem(item);
				}
		}

		virtual protected void DrawItem(IModularItem item) {
			item.Draw();
		}
	}

}
namespace Ktisis.Interface.Modular.ItemTypes.Splitter {
	public class Columns : BaseSplitter {
		public Columns(List<IModularItem> items) : base(items) { }

		public override void Draw() {
			if (this.Items != null) {
				ImGui.Columns(this.Items.Count);
				foreach (var item in this.Items) {
					this.DrawItem(item);
					ImGui.NextColumn();
				}
				ImGui.Columns();
			}

		}
	}

	public class BorderlessColumns : BaseSplitter {
		public BorderlessColumns(int windowID, string title, List<IModularItem> items) : base(windowID, title, items) { }

		public override void Draw() {
			if (this.Items != null) {
				ImGui.Columns(this.Items.Count, this.Title, false);
				foreach (var item in this.Items) {
					this.DrawItem(item);
					ImGui.NextColumn();
				}
				ImGui.Columns();
			}
		}
	}


	public class SameLine : BaseSplitter {
		public SameLine(List<IModularItem> items) : base(items) { }

		public override void Draw() {
			if (this.Items != null) {
				for (int i = 0; i < this.Items.Count; i++) {
					if (i != 0) ImGui.SameLine();
					this.DrawItem(this.Items[i]);
				}
			}
		}
	}
	public class CollapsibleHeader : BaseSplitter {
		public CollapsibleHeader(int windowID, string title, List<IModularItem> items) : base(windowID, title, items) { }

		public override void Draw() {
			if (this.Items != null)
				for (int i = 0; i < this.Items.Count; i++)
					if (ImGui.CollapsingHeader($"{this.Items[i].LocaleName()}##Window##{windowID}"))
						this.DrawItem(this.Items[i]);
		}
	}
}
