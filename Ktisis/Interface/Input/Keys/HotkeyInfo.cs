using System;

using Ktisis.Data.Config.Input;

namespace Ktisis.Interface.Input.Keys; 

public delegate bool HotkeyHandler();

[Flags]
public enum HotkeyFlags {
	None = 0,
	OnDown = 1,
	OnHeld = 2,
	OnRelease = 4
}

public class HotkeyInfo {
	public readonly string Name;
	public readonly HotkeyFlags Flags;
	public readonly HotkeyHandler Handler;

	public Keybind Keybind = new();
	
	public HotkeyInfo(string name, HotkeyHandler handler, HotkeyFlags flags = HotkeyFlags.OnDown) {
		this.Name = name;
		this.Flags = flags;
		this.Handler = handler;
	}
}
