using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface.Components; 

public class ObjectList {
	// Public draw methods
	
	public void Draw() {
		if (DrawFrame())
			ImGui.EndChildFrame();
	}

	// Draw outer frame

	private bool DrawFrame() {
		var style = ImGui.GetStyle();
		var avail = ImGui.GetContentRegionAvail().Y - style.FramePadding.Y * 2;
		return ImGui.BeginChildFrame(101, new Vector2(-1, avail), ImGuiWindowFlags.HorizontalScrollbar);
	}
}