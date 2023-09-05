using System.Linq;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects.World;
using Ktisis.Scene.Editing.Modes;
using Ktisis.Common.Utility;

namespace Ktisis.Interface.Overlay.Render; 

public class ObjectRenderer : RendererBase {
	// Draw
	
	public override void OnDraw(GuiOverlay overlay, ModeHandler handler) {
		var items = handler.GetEnumerator().Cast<WorldObject>();
		foreach (var item in items)
			DrawItem(overlay, item);
	}

	private void DrawItem(GuiOverlay overlay, WorldObject item) {
		if (!item.Visible || item is not IManipulable world)
			return;
		
		if (world.GetTransform() is Transform trans)
			overlay.Selection.AddItem(item, trans.Position);
	}
}
