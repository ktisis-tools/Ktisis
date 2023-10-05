using System;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Config.Input;

namespace Ktisis.Input.Factory; 

public class HotkeyAttribute : Attribute {
	public readonly string Name;
	public readonly HotkeyFlags Flags;
	
	public readonly Keybind Keybind;
	
	public HotkeyAttribute(
		string name,
		VirtualKey key = VirtualKey.NO_KEY,
		HotkeyFlags flags = HotkeyFlags.OnDown,
		params VirtualKey[] mods
	) {
		this.Name = name;
		this.Flags = flags;
		this.Keybind = new Keybind(key, mods);
	}
}
