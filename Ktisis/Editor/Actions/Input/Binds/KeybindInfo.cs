using System;

using Ktisis.Data.Config.Actions;

namespace Ktisis.Editor.Actions.Input.Binds;

[Flags]
public enum KeybindTrigger {
	None = 0,
	OnDown = 1,
	OnHeld = 2,
	OnRelease = 4
}

public class KeybindInfo {
	public KeybindTrigger Trigger = KeybindTrigger.None;
	public ActionKeybind Default = new();
}
