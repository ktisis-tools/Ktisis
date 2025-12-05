using System.Collections.Generic;

namespace Ktisis.Data.Config.Actions;

public class InputConfig {
	public bool BlockTargetLeftClick;
	public bool BlockTargetRightClick;
	public bool ScrollModifier;
	public bool ScrollAllow = true;
	public bool Enabled;

	public Dictionary<string, ActionKeybind> Keybinds = new();

	public ActionKeybind GetOrSetDefault(string name, ActionKeybind defaultValue) {
		if (this.Keybinds.TryGetValue(name, out var bind))
			return bind;
		this.Keybinds.Add(name, defaultValue);
		return defaultValue;
	}
}
