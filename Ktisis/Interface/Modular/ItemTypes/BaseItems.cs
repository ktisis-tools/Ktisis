using System.Collections.Generic;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes {
	public class BaseContainer : IModularItem, IModularContainer {
		public List<IModularItem> Items { get; }
		public ParamsExtra Extra { get; set; }
		protected int Id;
		public string? Title { get; set; }
		public string LocaleHandle { get; set; }
		public ImGuiWindowFlags WindowFlags { get; set; }

		public BaseContainer(List<IModularItem> items, ParamsExtra extra) {
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

			if (Extra.Strings != null && Extra.Strings.TryGetValue("Title", out string? title))
				if (title != null)
					this.Title = title;
		}
		public BaseContainer(List<IModularItem> items, ParamsExtra extra, ImGuiWindowFlags windowFlags) : this(items, extra) {
			this.AddWindowFlags(windowFlags);
		}

		private void AddWindowFlags(ImGuiWindowFlags windowFlags) {
			this.WindowFlags |= windowFlags;
			if (this.Extra.Ints.TryGetValue("WindowFlags", out int _))
				this.Extra.Ints["WindowFlags"] |= (int)this.WindowFlags;
			else
				this.Extra.Ints.Add("WindowFlags", (int)this.WindowFlags);
		}

		virtual public string LocaleName() => Locale.GetString(this.LocaleHandle);
		virtual public string GetTitle() => $"{this.Title ?? this.LocaleName()}##Modular##Item##{this.Id}";
		virtual public void Draw() {
			if (ImGui.Begin(this.GetTitle(), this.WindowFlags)) {
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

	public class BasePannel : IModularItem {
		public ParamsExtra Extra { get; set; }
		protected int Id;
		public string? Title { get; set; }
		public string LocaleHandle { get; set; }

		public BasePannel(ParamsExtra extra, string? localeHandle = null) {
			this.Extra = extra;

			extra.Strings?.TryGetValue("LocaleHandle", out localeHandle);
			this.LocaleHandle = localeHandle ?? "ModularPanel";

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

		virtual public void Draw() { }
	}



}
