using Dalamud.Game.ClientState.Keys;

using Ktisis.Data.Config.Input;

namespace Ktisis.Interface.Input.Keys;

public class HotkeyFactory {
	private readonly string Name;
	private readonly HotkeyHandler Handler;

	private HotkeyFlags Flags = HotkeyFlags.OnDown;

	private Keybind? Keybind;
	
	public HotkeyFactory(string name, HotkeyHandler handler) {
		this.Name = name;
		this.Handler = handler;
	}

	public HotkeyFactory SetFlags(HotkeyFlags flags) {
		this.Flags = flags;
		return this;
	}

	public HotkeyFactory SetFlag(HotkeyFlags flag) {
		this.Flags |= flag;
		return this;
	}

	public HotkeyFactory SetDefaultKey(VirtualKey key, params VirtualKey[] mods) {
		this.Keybind = new Keybind(key, mods);
		return this;
	}
	
	public Keybind? GetDefaultKey() => this.Keybind;

	public HotkeyInfo Create() {
		return new HotkeyInfo(
			this.Name,
			this.Handler,
			this.Flags
		);
	}
}
