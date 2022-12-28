using System.Collections.Generic;

using Ktisis.Interface.Workspace;
using Ktisis.Structs.Actor;

namespace Ktisis.Services {
	public class EditorService {
		public static List<Manipulable> Selections = new();

		public static Manipulable? Selection {
			get => Selections.Count > 0 ? Selections[0] : null;
			set {
				Selections.Clear();
				if (value != null) Selections.Add(value);
			}
		}

		public static void Select(Manipulable target, bool add = false) {
			if (!add) Selections.Clear();
			Selections.Add(target);
		}

		public unsafe static ActorObject? GetTargetManipulable() {
			var tar = Ktisis.GPoseTarget;
			if (tar == null) return null;

			var actor = (Actor*)tar.Address;
			return new ActorObject(actor->ObjectID);
		}

		public static bool IsSelected(Manipulable target) {
			foreach (var item in Selections)
				if (item == target) return true;
			return false;
		}
	}
}