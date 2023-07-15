using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Core;
using Ktisis.Scenes;
using Ktisis.Interface.Widgets;
using Ktisis.Interface.SceneUi;

namespace Ktisis.Interface.Windows; 

public class Workspace : GuiWindow {
	// Constants

	private readonly static Vector2 MinimumSize = new(280, 300);
	
	// Singleton access

	private readonly SceneManager? SceneManager;
	
	// Constructor
	
	public Workspace(Gui gui) : base(gui, "Ktisis Workspace") {
		SceneManager = Ktisis.Singletons.Get<SceneManager>();
		SceneTree = new SceneTree(SceneManager);
		
		RespectCloseHotkey = false;
	}
	
	// Components

	private readonly SceneTree SceneTree;
	
	// Draw window contents

	public override void Draw() {
		if (SceneManager == null) {
			DrawError();
			return;
		}
		
		// Set size constrains

		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};

		var scene = SceneManager.Scene;
		ImGui.BeginDisabled(scene == null);

		DrawStateFrame(scene);

		ImGui.Spacing();

		var style = ImGui.GetStyle();
		var bottomHeight = UiBuilder.IconFont.FontSize + (style.ItemSpacing.Y + style.ItemInnerSpacing.Y) * 2;
		var treeHeight = ImGui.GetContentRegionAvail().Y - bottomHeight;
		SceneTree.Draw(treeHeight);

		ImGui.Spacing();

		DrawTreeButtons();
		
		ImGui.EndDisabled();
	}

	private void DrawStateFrame(Scene? scene) {
		var style = ImGui.GetStyle();
		var height = (ImGui.GetFontSize() + style.ItemInnerSpacing.Y) * 2 + style.ItemSpacing.Y;
		
		var result = ImGui.BeginChildFrame(102, new Vector2(-1, height));
		if (!result)
			return;
		
		try {
			if (scene != null)
				DrawStateInfo(scene, height);
			else
				ImGui.Text("Waiting for scene...");
		} finally {
			ImGui.EndChildFrame();
		}
	}

	private void DrawStateInfo(Scene scene, float height) {
		var padding = ImGui.GetStyle().FramePadding.X;
		
		// Actor name + selection state
		
		ImGui.BeginGroup();

		ImGui.SetCursorPosX(padding * 2);

		var tar = Services.Game.GPose.GetTarget();
		ImGui.Text(tar != null ? tar.Name.TextValue : "No target found!");
		
		ImGui.SetCursorPosX(padding * 2);

		var ct = SceneManager!.SelectOrder.Count;
		if (ct > 0) {
			ImGui.BeginDisabled();
			ImGui.Text($"{ct} item{(ct == 1 ? "" : "s")} selected.");
			ImGui.EndDisabled();
		} else {
			ImGui.TextDisabled("No items selected.");
		}

		ImGui.EndGroup();
		
		// Overlay toggle

		ImGui.SameLine();
		
		var overlay = Gui.Overlay.Visible;

		const float ratio = 0.75f;
		
		var btnSize = new Vector2(height, height) * ratio;
		ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - padding - btnSize.X);
		ImGui.SetCursorPosY(height * (1 - ratio) / 2);
		if (Buttons.DrawIconButton(overlay ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, btnSize))
			Gui.Overlay.Visible = !Gui.Overlay.Visible;
	}

	private void DrawTreeButtons() {
		Buttons.DrawIconButton(FontAwesomeIcon.Plus);
		ImGui.SameLine();
		Buttons.DrawIconButton(FontAwesomeIcon.Filter);
	}

	private void DrawError() {
		var cursorY = ImGui.GetCursorPosY();
		ImGui.SetCursorPosY(cursorY + UiBuilder.DefaultFont.FontSize / 2);
		Icons.DrawIcon(FontAwesomeIcon.Frown);
		ImGui.SameLine();
		ImGui.SetCursorPosY(cursorY);
		ImGui.Text("Scene manager not found!\nKtisis may have experienced an error while loading.");
		ImGui.Spacing();
		ImGui.Text("Please forward any error logs to the plugin's developers for debugging.");
		Flags |= ImGuiWindowFlags.AlwaysAutoResize;
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(-1, -1)
		};
	}
}