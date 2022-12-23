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
}