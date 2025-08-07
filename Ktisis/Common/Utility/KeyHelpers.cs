using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;

namespace Ktisis.Common.Utility;

public static class KeyHelpers {
	public static bool IsModifierKey(VirtualKey key) => key is VirtualKey.CONTROL or VirtualKey.SHIFT or VirtualKey.MENU;
	
	public static IEnumerable<VirtualKey> GetKeysDown() {
		var io = ImGui.GetIO();

		if (io.KeyCtrl) yield return VirtualKey.CONTROL;
		if (io.KeyShift) yield return VirtualKey.SHIFT;
		if (io.KeyAlt) yield return VirtualKey.MENU;
		
		for (var i = 0; i < io.KeysDown.Length; i++) {
			if (!io.KeysDown[i]) continue;
			
			var key = ImGuiHelpers.ImGuiKeyToVirtualKey((ImGuiKey)i);
			if (key is >= VirtualKey.LCONTROL and <= VirtualKey.RMENU)
				continue;
			
			if (key != VirtualKey.NO_KEY)
				yield return key;
		}
	}
}
