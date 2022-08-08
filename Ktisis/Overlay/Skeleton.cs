using System;

using ImGuiNET;

using Dalamud.Logging;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

namespace Ktisis.Overlay {
	public sealed class Skeleton {
		public GameGui Gui;

		public GameObject? Subject;

		public Skeleton(GameGui gui, GameObject? subject) {
			Gui = gui;
			Subject = subject;
		}

		public unsafe ActorModel* GetSubjectModel() {
			return ((Actor*)Subject?.Address)->Model;
		}

		public unsafe void Draw() {
			if (Subject == null)
				return;

			if (!Gui.WorldToScreen(Subject.Position, out var pos))
				return;

			var model = GetSubjectModel();
			if (model == null)
				return;

			
		}
	}
}
