using FFXIVClientStructs.FFXIV.Client.Game.Control;

using ImGuiNET;

using Ktisis.Scenes;
using Ktisis.Scenes.Objects;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.SceneUi.Logic;

namespace Ktisis.Interface.SceneUi; 

internal class SceneRender {
	// Dependency access
	
	private readonly GuiOverlay Overlay;
	private readonly SceneManager SceneManager;

	// Constructor

	internal SceneRender(GuiOverlay overlay, SceneManager scene) {
		Overlay = overlay;
		SceneManager = scene;
	}

	// Draw all objects in scene

	private Gizmo? Gizmo;
	
	internal void Draw(Gizmo? gizmo) {
		Gizmo = gizmo;
		
		var scene = SceneManager.Scene;
		scene?.Children.ForEach(DrawItem);
	}

	private void DrawItem(SceneObject item) {
		if (item is IOverlay ov) {
			if (ov.CanDraw) return;
			ov.Draw();
		}

		if (item.Selected && item is IManipulable manip && Gizmo is Gizmo gizmo) {
			var compose = manip.ComposeMatrix();
			if (compose != null) {
				var mx = compose.Value;
				if (gizmo.Manipulate(ref mx))
					manip.SetMatrix(mx);
			}
		}
		
		item.Children.ForEach(DrawItem);
	}
}