using System.Collections.Generic;

using ImGuiNET;

namespace Ktisis.Interface.Modular.ItemTypes.Splitter {
	public class Columns : BaseSplitter {
		public Columns(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

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
		public BorderlessColumns(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

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
		public SameLine(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

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
		public CollapsibleHeader(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

		public override void Draw() {
			if (this.Items != null)
				for (int i = 0; i < this.Items.Count; i++)
					if (ImGui.CollapsingHeader(this.Items[i].GetTitle()))
						this.DrawItem(this.Items[i]);
		}
	}
	public class Tabs : BaseSplitter {
		public Tabs(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

		public override void Draw() {
			if (this.Items != null)
				if (ImGui.BeginTabBar(GetTitle()))
					for (int i = 0; i < this.Items.Count; i++)
						if (ImGui.BeginTabItem(this.Items[i].GetTitle())) {
							this.DrawItem(this.Items[i]);
							ImGui.EndTabItem();
						}

		}
	}
	public class Group : BaseSplitter {
		public Group(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }
	}
}
