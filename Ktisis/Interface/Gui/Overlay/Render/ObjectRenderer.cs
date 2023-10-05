using System.Linq;

using Ktisis.Common.Utility;
using Ktisis.Editing.Modes;
using Ktisis.Scene.Objects.World;

namespace Ktisis.Interface.Gui.Overlay.Render; 

public class ObjectRenderer : RendererBase {
	// Draw
	
	public override void OnDraw(GuiOverlay overlay, ModeHandler handler) {
		var items = handler.GetEnumerator().Cast<WorldObject>();
		foreach (var item in items)
			DrawItem(overlay, item);
	}

	private void DrawItem(GuiOverlay overlay, WorldObject item) {
		if (!item.Visible) return;
		
		if (item.GetTransform() is Transform trans)
			overlay.Selection.AddItem(item, trans.Position);
	}
}
