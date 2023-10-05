using Ktisis.Editing.Modes;

namespace Ktisis.Interface.Gui.Overlay.Render;

public abstract class RendererBase {
	public abstract void OnDraw(GuiOverlay overlay, ModeHandler handler);
}
