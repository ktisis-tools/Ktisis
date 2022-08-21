using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface {
	internal unsafe class CustomizeUI {
		Ktisis Plugin;

		public bool Visible = false;

		public Actor* Target;

		// Constructor

		public CustomizeUI(Ktisis plugin) {
			Plugin = plugin;
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Set target

		public unsafe void SetTarget(Actor* actor) {
			Target = actor;
		}

		public unsafe void SetTarget(GameObject actor) {
			SetTarget((Actor*)actor.Address);
		}

		// Draw window

		public void Draw() {
			if (!Visible)
				return;
		}
	}
}
