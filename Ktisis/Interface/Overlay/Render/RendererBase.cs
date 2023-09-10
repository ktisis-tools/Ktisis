using Ktisis.Editing.Modes;

namespace Ktisis.Interface.Overlay.Render;

public abstract class RendererBase {
	public abstract void OnDraw(GuiOverlay overlay, ModeHandler handler);
}
