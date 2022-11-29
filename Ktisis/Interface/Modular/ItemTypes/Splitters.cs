using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Interface.Components;

namespace Ktisis.Interface.Modular.ItemTypes {

	public class BaseSplitter : IModularItem, IModularContainer {
		public List<IModularItem> Items { get; }

		public BaseSplitter(List<IModularItem> items) => this.Items = items;

		public void Draw() {
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

		public new void Draw() {
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

}
