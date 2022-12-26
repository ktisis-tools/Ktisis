using Dalamud.Game;

using Ktisis.Services;

namespace Ktisis.Structs.Actor.State {
	public static class ActorStateWatcher {

		private static bool _wasInGPose = false;

		public static void Dispose() {
			DalamudServices.Framework.Update -= Monitor;
			if(Ktisis.IsInGPose)
				EventService.OnGPoseChange!.Invoke(false);
		}

		public static void Init() {
			DalamudServices.Framework.Update += Monitor;
		}

		public static void Monitor(Framework framework) {
			if (_wasInGPose != Ktisis.IsInGPose) {
				_wasInGPose = Ktisis.IsInGPose;
				EventService.OnGPoseChange!.Invoke(Ktisis.IsInGPose);
			}
		}
	}
}
