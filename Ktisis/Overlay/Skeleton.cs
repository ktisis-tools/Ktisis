using System;

using Dalamud.Game.Gui;
using Dalamud.Game.ClientState.Objects.Types;

using ImGuiNET;

namespace Ktisis.Overlay {
	public sealed class Skeleton {
		public GameGui Gui;

		public GameObject? Subject;

		public Skeleton(GameGui gui, GameObject? subject) {
			Gui = gui;
			Subject = subject;
		}

		public void Draw() {
			if (Subject == null)
				return;

			if (!Gui.WorldToScreen(Subject.Position, out var pos))
				return;
		}
	}
}
