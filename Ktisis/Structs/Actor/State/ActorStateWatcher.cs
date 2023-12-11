using System.Collections.Generic;
using System.Linq;

using Ktisis.Events;

using Dalamud.Plugin.Services;

using Ktisis.Data.Files;

namespace Ktisis.Structs.Actor.State {
	public static class ActorStateWatcher {
		private static bool _wasInGPose = false;

		private static readonly Dictionary<ushort, AnamCharaFile> _originalActorData = new();

		public static void Dispose() {
			Services.Framework.Update -= Monitor;
			if(Ktisis.IsInGPose)
				EventManager.FireOnGposeChangeEvent(false);
		}

		public static void Init() {
			Services.Framework.Update += Monitor;
		}

		private static void Monitor(IFramework framework) {
			if (_wasInGPose != Ktisis.IsInGPose)
				HandleStateChanged(Ktisis.IsInGPose);
		}
		
		private unsafe static void HandleStateChanged(bool state) {
			_wasInGPose = state;
			EventManager.FireOnGposeChangeEvent(state);

			if (state) { // Enter
				foreach (var gameObj in Services.ObjectTable.Where(go => go.ObjectIndex <= 248)) {
					var actor = (Actor*)gameObj.Address;
					if (actor == null) continue;
					
					Logger.Debug($"Saving actor data for {actor->GameObject.ObjectIndex} {actor->GetName()}");

					var save = new AnamCharaFile();
					save.WriteToFile(*actor, AnamCharaFile.SaveModes.All);
					_originalActorData.Add(gameObj.ObjectIndex, save);
				}
			} else { // Exit
				_originalActorData.Clear();
			}
		}

		public unsafe static void RevertToOriginal(Actor* actor) {
			if (actor == null) return;

			if (!_originalActorData.TryGetValue(actor->GameObject.ObjectIndex, out var save))
				return;

			save.Apply(actor, AnamCharaFile.SaveModes.All);
		}
	}
}
