using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes {
	public class BaseSplitter : IModularItem, IModularContainer {
		public List<IModularItem> Items { get; }
		public ParamsExtra Extra { get; set; }
		protected int Id;
		public string? Title { get; set; }
		public string LocaleHandle { get; set; }

		protected BaseSplitter(List<IModularItem> items, ParamsExtra extra) {
			this.Items = items;
			this.Extra = extra;

			string? localeHandle = null;
			extra.Strings?.TryGetValue("LocaleHandle", out localeHandle);
			this.LocaleHandle = localeHandle ?? "ModularSplitter";


			if (extra!.Ints!.TryGetValue("Id", out int windowId))
				this.Id = windowId;
			else
				this.Id = 1120;

			if (Extra.Strings != null && Extra.Strings.TryGetValue("Title", out string? title))
				if (title != null)
					this.Title = title;
		}

		virtual public string LocaleName() => Locale.GetString(this.LocaleHandle);
		virtual public string GetTitle() => $"{this.Title ?? this.LocaleName()}##Modular##Item##{this.Id}";
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
					if (ImGui.CollapsingHeader($"{this.Items[i].LocaleName()}##Window##{Id}"))
						this.DrawItem(this.Items[i]);
		}
	}
	public class Tabs : BaseSplitter {
		public Tabs(List<IModularItem> items, ParamsExtra extra) : base(items, extra) { }

		public override void Draw() {
			if (this.Items != null)
				if (ImGui.BeginTabBar(GetTitle()))
					for (int i = 0; i < this.Items.Count; i++)
						if (ImGui.BeginTabItem($"{this.Items[i].LocaleName()}##Modular##{i}##{Id}")) {
							this.DrawItem(this.Items[i]);
							ImGui.EndTabItem();
						}

		}
	}
}
