using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Ktisis.Common.Utility;

public static class KeyHelpers {
	public static bool IsModifierKey(VirtualKey key) => key is VirtualKey.CONTROL or VirtualKey.SHIFT or VirtualKey.MENU;
	
	public static IEnumerable<VirtualKey> GetKeysDown() {
		var io = ImGui.GetIO();

		if (io.KeyCtrl) yield return VirtualKey.CONTROL;
		if (io.KeyShift) yield return VirtualKey.SHIFT;
		if (io.KeyAlt) yield return VirtualKey.MENU;
		
		for (var i = 0; i < io.KeysDown.Count; i++) {
			if (!io.KeysDown[i]) continue; // 135 = F24
			var key = ImGuiHelpers.ImGuiKeyToVirtualKey((ImGuiKey)i);
			if (key != VirtualKey.NO_KEY && key <= VirtualKey.F24)
				yield return key;
		}
	}
}
