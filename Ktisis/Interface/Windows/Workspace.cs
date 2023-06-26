using System.Numerics;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Interface.Common;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows;

public class Workspace : GuiWindow {
	// Constants

	private readonly static Vector2 MinimumSize = new(300, 200);

	// Constructor

	public Workspace(Gui gui) : base(gui, "Ktisis Workspace") {
		RespectCloseHotkey = false;
	}

	// Components

	private readonly ObjectList ObjectList = new();

	// Draw window contents

	public override void Draw() {
		// Set size constrains

		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};

		var scene = Ktisis.Singletons.Get<SceneManager>().Scene;
		ImGui.BeginDisabled(scene == null);

		ObjectList.Draw();

		ImGui.EndDisabled();
	}
}
