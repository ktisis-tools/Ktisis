using System.Linq;

using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Data.Config.Input; 

public class Keybind {
	public VirtualKey? Key;
	public VirtualKey[] Mod;

	public Keybind(VirtualKey key = VirtualKey.NO_KEY, params VirtualKey[] mods) {
		this.Key = key;
		this.Mod = mods;
	}

	public string GetShortcutString() {
		if (this.Key == null)
			return string.Empty;

		var keys = this.Mod.Append(this.Key.Value)
			.Select(key => key.GetFancyName());
		return string.Join(" + ", keys);
	}
}
