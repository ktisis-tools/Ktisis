using System.Linq;

using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Editor.Actions.Input.Binds;

public class KeyCombo {
	public VirtualKey Key;
	public VirtualKey[] Modifiers;

	public KeyCombo(VirtualKey key = VirtualKey.NO_KEY, params VirtualKey[] mods) {
		this.Key = key;
		this.Modifiers = mods;
	}

	public string GetShortcutString() {
		var keys = this.Modifiers.Append(this.Key)
			.Select(key => key.GetFancyName());
		return string.Join(" + ", keys);
	}
}
