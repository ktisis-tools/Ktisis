using Dalamud.Bindings.ImGui;

namespace Ktisis.Interface.Widgets;

public static class InputUInt {
	public static bool Draw(string label, ref uint value) {
		var intValue = (int)value;
		var result = ImGui.InputInt(label, ref intValue);
		if (result) value = (uint)intValue;
		return result;
	}
}
