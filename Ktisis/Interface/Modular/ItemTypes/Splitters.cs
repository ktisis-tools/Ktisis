using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Interface.Components;

namespace Ktisis.Interface.Modular.ItemTypes {

	public class BaseSplitter : IModularItem, IModularContainer {
		public List<IModularItem> Items { get; }

		public BaseSplitter(List<IModularItem> items) => this.Items = items;

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

}
