using System;

using ImGuiNET;

using Dalamud.Interface.Windowing;

using Ktisis.Interface.Windows;

namespace Ktisis.Interface {
    public static class KtisisGui {
		public static WindowSystem Windows = new("Ktisis");

		static KtisisGui() {
			Windows.AddWindow(new Sidebar());
		}

		public static void Draw() => Windows.Draw();

		public static Window? GetWindow(string name) => Windows.GetWindow(name);
	}

	public abstract class KtisisWindow : Window {
		public KtisisWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow) {
			var exists = KtisisGui.GetWindow(name);
			if (exists != null)
				KtisisGui.Windows.RemoveWindow(exists);
			KtisisGui.Windows.AddWindow(this);
		}

		public void Show() => IsOpen = true;
		public void Close() => IsOpen = false;

		public override void OnClose() => KtisisGui.Windows.RemoveWindow(this);
	}
}