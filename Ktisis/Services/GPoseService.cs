using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Actor;

using System.Collections.Generic;

namespace Ktisis.Services {
	public static class GPoseService {
		public static bool IsInGPose => DalamudServices.PluginInterface.UiBuilder.GposeActive && IsGposeTargetPresent();

		public unsafe static Actor* TargetActor => (Actor*)DalamudServices.Targets->GPoseTarget;
		public unsafe static GameObject? TargetObject => DalamudServices.ObjectTable.CreateObjectReference((nint)TargetActor);

		public unsafe static bool IsGposeTargetPresent() => (nint)TargetActor != 0;

		public static List<GameObject> GetGPoseActors() {
			var results = new List<GameObject>();

			foreach (var actor in DalamudServices.ObjectTable)
				if (actor.ObjectIndex > 200 && actor.ObjectIndex < 248)
					results.Add(actor);

			return results;
		}
	}
}