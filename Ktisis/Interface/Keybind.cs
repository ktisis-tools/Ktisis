using System.Linq;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Structs.Input;

namespace Ktisis.Interface {
	public class Keybind {
		public VirtualKey[] Keys = {};

		public Keybind(params VirtualKey[] keys) => Keys = keys;

		public string Display() => Keys.Length switch {
			0 => "None",
			_ => string.Join("+", Keys.Select(key => key.GetFancyName()))
		};

		public unsafe bool IsActive(KeyboardState* state) {
			var active = Keys.Length > 0;
			foreach (var key in Keys)
				active &= state->IsKeyDown(key, true);
			return active;
		}
	}
}