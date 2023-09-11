using Dalamud.Game.ClientState.Keys;

namespace Ktisis.Data.Config.Input; 

public class Keybind {
	public VirtualKey? Key;
	public VirtualKey[] Mod;

	public Keybind(VirtualKey? key, params VirtualKey[] mods) {
		this.Key = key;
		this.Mod = mods;
	}
}
