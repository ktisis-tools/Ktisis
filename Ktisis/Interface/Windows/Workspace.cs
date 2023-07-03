using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Scenes;
using Ktisis.Interface.Widgets;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows; 

public class Workspace : GuiWindow {
	// Constants

	private readonly static Vector2 MinimumSize = new(280, 300);
	
	// Constructor
	
	public Workspace(Gui gui) : base(gui, "Ktisis Workspace") {
		RespectCloseHotkey = false;
	}
	
	// Components

	private readonly ItemTree ItemTree = new();
	
	// Draw window contents

	public override void Draw() {
		// Set size constrains

		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};

		var scene = Ktisis.Singletons.Get<SceneManager>().Scene;
		ImGui.BeginDisabled(scene == null);

		var style = ImGui.GetStyle();
		
		var bottomHeight = UiBuilder.IconFont.FontSize + (style.ItemSpacing.Y + style.ItemInnerSpacing.Y) * 2;
		var treeHeight = ImGui.GetContentRegionAvail().Y - bottomHeight;
		ItemTree.Draw(scene, treeHeight);

		ImGui.Spacing();

		DrawTreeButtons();
		
		ImGui.EndDisabled();
	}

	private void DrawTreeButtons() {
		Buttons.DrawIconButton(FontAwesomeIcon.Plus);
		ImGui.SameLine();
		Buttons.DrawIconButton(FontAwesomeIcon.Filter);
	}
}