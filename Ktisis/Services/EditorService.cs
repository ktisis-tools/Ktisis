using System.Collections.Generic;

using Ktisis.Scene;
using Ktisis.Scene.Actors;
using Ktisis.Structs.Actor;
using Ktisis.Interface;
using Ktisis.Interface.Windows;
using Ktisis.Services;

namespace Ktisis.Services {
	public static class EditorService {
		// Properties

		public static List<Manipulable> Items = new();
		public static List<Manipulable> Selections = new();

		// Constructor

		public static void Init() {
			EventService.OnGPoseChange += OnGPoseChange;

			if (Ktisis.Configuration.OpenKtisisMethod == OpenKtisisMethod.OnPluginLoad)
				KtisisGui.GetWindowOrCreate<Sidebar>().Show();
		}

		private static void OnGPoseChange(bool state) {
			if (state) {
				FindTarget(true);
			} else {
				Items.Clear();
			}

			if (Ktisis.Configuration.OpenKtisisMethod == OpenKtisisMethod.OnEnterGpose)
				KtisisGui.GetWindowOrCreate<Sidebar>().Show();
		}

		// Item selection

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

		public static bool IsSelected(Manipulable target) {
			foreach (var item in Selections)
				if (item == target) return true;
			return false;
		}

		// Create ActorObject for GPose target

		public unsafe static ActorObject? GetTargetManipulable() {
			var tar = GPoseService.GPoseTarget;
			if (tar == null) return null;

			var actor = (Actor*)tar.Address;
			return new ActorObject(actor->ObjectID);
		}

		internal unsafe static ActorObject? FindTarget(bool append = false) {
			var tar = GPoseService.GPoseTarget;
			if (tar == null) return null;

			var ptr = (Actor*)tar.Address;

			foreach (var item in Items) {
				if (item is ActorObject actor) {
					if (actor.GetActor() == ptr)
						return actor;
				} else continue;
			}

			if (append) {
				var item = GetTargetManipulable();
				if (item == null) return null;
				Items.Add(item);
				return item;
			}

			return null;
		}
	}
}