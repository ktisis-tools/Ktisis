using System;
using System.Reflection;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface.Windowing;

namespace Ktisis.Interface {
	public static class KtisisGui {
		public static WindowSystem Windows = new("Ktisis");

		public static void Draw() => Windows.Draw();

		// Get window

		private static readonly FieldInfo windowsField = typeof(WindowSystem).GetField("windows", BindingFlags.Instance | BindingFlags.NonPublic)!;

		public static KtisisWindow? GetWindow<T>() {
			var v = (List<Window>?)windowsField.GetValue(Windows);
			if (v != null) {
				foreach (var w in v) {
					if (w is T) return w as KtisisWindow;
				}
			}
			return null;
		}

		public static KtisisWindow GetWindowOrCreate<T>(object[]? args = null)
			=> GetWindow<T>() ?? (KtisisWindow)Activator.CreateInstance(typeof(T), args)!;

		public static Window? GetWindowByName(string name)
			=> Windows.GetWindow(name);
	}

	public abstract class KtisisWindow : Window {
		public KtisisWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : base(name, flags, forceMainWindow) {
			var exists = KtisisGui.GetWindowByName(name);
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