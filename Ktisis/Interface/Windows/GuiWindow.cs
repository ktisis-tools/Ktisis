using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Ktisis.Interface.Windows;

public abstract class GuiWindow : Window {
	protected readonly Gui Gui;

	protected GuiWindow(
		Gui gui,
		string name,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None,
		bool forceMainWindow = false
	) : base(name, flags, forceMainWindow) {
		Gui = gui;
	}

	public override void OnClose() {
		base.OnClose();
		Gui.RemoveWindow(this);
	}
}
