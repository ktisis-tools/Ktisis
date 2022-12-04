using System.Collections.Generic;
using System.Linq;

using ImGuiNET;

using Ktisis.Localization;

namespace Ktisis.Interface.Modular.ItemTypes {

	public class BaseItem : IModularItem {
		protected int Id;
		public string? Title { get; set; }
		public string LocaleHandle { get; set; }

		public BaseItem() {
			Id = GenerateId();
			Title = null;
			LocaleHandle = "ModularItem";
		}
		private static int GenerateId() {
			int id = 0;
			if (Manager.ItemIds.Any())
				id = Manager.ItemIds.Max() + 1;
			Manager.ItemIds.Add(id);
			return id;
		}

		virtual public void DrawConfig() {
			string title = Title ?? "";
			if (ImGui.InputText($"Title##ModularItem#Field", ref title, 200))
				Title = title;
		}

		virtual public string LocaleName() => Locale.GetString(this.LocaleHandle);
		virtual public string GetTitle() => $"{this.Title ?? this.LocaleName()}##Modular##Item##{this.Id}";

		// virtual methods
		virtual public void Draw() { }

	}
	public class BaseContainer : BaseItem, IModularContainer {
		public List<IModularItem> Items { get; set; } = new();
		public BaseContainer() : base() { }

		override public void Draw() {
			if (this.Items != null)
				foreach (var item in this.Items) {
					this.DrawItem(item);
				}
		}
		virtual protected void DrawItem(IModularItem item) => item.Draw();
	}

	public class BaseSplitter : BaseItem, IModularContainer {
		public List<IModularItem> Items { get; set; } = new();

		override public void Draw() {
			if (this.Items != null)
				foreach (var item in this.Items) {
					this.DrawItem(item);
				}
		}

		virtual protected void DrawItem(IModularItem item) => item.Draw();
	}

	public class BasePannel : BaseItem {
	}



	/////////////////
	// Items ideas //
	/////////////////

	// indicator for bone cat overload (with initials maybe)
	// individual buttons for siblings and  ControlButton extras
	// window open/toggle => it would be able to have containers as children
	// Sameline X space configurable (float)

	///////////////////
	// Concept ideas //
	///////////////////

	// force fix width to prevent align right
	// improve drag drop

}
