using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes {

	public class BaseItem : IModularItem {
		public ParamsExtra Extra { get; set; }
		protected int Id;
		public string? Title { get; set; }
		public string LocaleHandle { get; set; }

		public BaseItem(ParamsExtra extra) {
			this.Extra = extra;

			string? localeHandle = null;
			Extra.Strings?.TryGetValue("LocaleHandle", out localeHandle);
			this.LocaleHandle = localeHandle ?? "ModularItem";

			if (Extra.Ints.TryGetValue("Id", out int windowId))
				this.Id = windowId;
			else
				this.Id = 1023;

			if (Extra.Strings != null && Extra.Strings.TryGetValue("Title", out string? title))
				if (title != null)
					this.Title = title;

		}

		virtual public string LocaleName() => Locale.GetString(this.LocaleHandle);
		virtual public string GetTitle() => $"{this.Title ?? this.LocaleName()}##Modular##Item##{this.Id}";

		// virtual methods
		virtual public void Draw() { }

	}

	public class BaseContainer : BaseItem, IModularContainer {
		public List<IModularItem> Items { get; }
		public ImGuiWindowFlags WindowFlags { get; set; }

		public BaseContainer(List<IModularItem> items, ParamsExtra extra) : base(extra) {
			this.Items = items;

			if (Extra.Ints.TryGetValue("WindowFlags", out int drawFlags))
				this.WindowFlags = (ImGuiWindowFlags)drawFlags;
			else
				this.WindowFlags = ImGuiWindowFlags.None;
		}
		public BaseContainer(List<IModularItem> items, ParamsExtra extra, ImGuiWindowFlags windowFlags) : this(items, extra) {
			this.AddWindowFlags(windowFlags);
		}

		private void AddWindowFlags(ImGuiWindowFlags windowFlags) {
			if (!this.Extra.Ints.ContainsKey("WindowFlags")) {
				this.WindowFlags |= windowFlags;
				this.Extra.Ints.Add("WindowFlags", (int)this.WindowFlags);
			}
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

		virtual protected void DrawItem(IModularItem item) => item.Draw();
	}

	public class BaseSplitter : BaseItem, IModularContainer {
		public List<IModularItem> Items { get; }
		protected BaseSplitter(List<IModularItem> items, ParamsExtra extra) : base(extra) {
			this.Items = items;
		}

		override public void Draw() {
			if (this.Items != null)
				foreach (var item in this.Items) {
					this.DrawItem(item);
				}
		}

		virtual protected void DrawItem(IModularItem item) => item.Draw();
	}

	public class BasePannel : BaseItem {
		public BasePannel(ParamsExtra extra, string? localeHandle = null) : base(extra) {
			this.Extra = extra;

			if (localeHandle != null)
				this.LocaleHandle = localeHandle;
		}
	}
}
