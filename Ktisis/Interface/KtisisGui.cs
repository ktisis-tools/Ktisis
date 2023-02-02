using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface.Windowing;

using Ktisis.Interface.Overlay;

namespace Ktisis.Interface {
	public static class KtisisGui {
		public static int SequenceId = 0;

		public static WindowSystem Windows = new("Ktisis");

		public static void Draw() {
			SequenceId = 0;

			Windows.Draw();

			GuiOverlay.Draw();
		}

		// Get window

		private static readonly FieldInfo WindowsField = typeof(WindowSystem).GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;
		private static List<Window> WindowsList() => (List<Window>?)WindowsField.GetValue(Windows)!;

		public static KtisisWindow? GetWindow<T>()
			=> (KtisisWindow?)WindowsList().FirstOrDefault(window => window is T);

		public static KtisisWindow? GetWindow(Type t)
			=> (KtisisWindow?)WindowsList().FirstOrDefault(window => window.GetType() == t);

		public static KtisisWindow GetWindowOrCreate<T>(object[]? args = null)
			=> GetWindow<T>() ?? (KtisisWindow)Activator.CreateInstance(typeof(T), args)!;
	}

	public abstract class KtisisWindow : Window {
		public KtisisWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow) {
			var exists = KtisisGui.GetWindow(GetType());
			if (exists != null) {
				IsOpen = true;
				KtisisGui.Windows.RemoveWindow(exists);
			}
			KtisisGui.Windows.AddWindow(this);
		}

		public void Show() => IsOpen = true;
		public void Close() => IsOpen = false;

		public void ToggleOnOrRemove() {
			if (IsOpen) {
				Close();
				OnClose();
			} else {
				Show();
			}
		}

		public override void OnClose() => KtisisGui.Windows.RemoveWindow(this);
	}
}