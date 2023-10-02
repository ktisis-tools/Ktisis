using Dalamud.Game;
using Ktisis.Events;
using System;

using Dalamud.Plugin.Services;

namespace Ktisis.Structs.Actor.State {
	public static class ActorStateWatcher {

		private static bool _wasInGPose = false;

		public static void Dispose() {
			Services.Framework.Update -= Monitor;
			if(Ktisis.IsInGPose)
				EventManager.FireOnGposeChangeEvent(false);
		}

		public static void Init() {
			Services.Framework.Update += Monitor;
		}

		public static void Monitor(IFramework framework) {
			if (_wasInGPose != Ktisis.IsInGPose) {
				_wasInGPose = Ktisis.IsInGPose;
				EventManager.FireOnGposeChangeEvent(Ktisis.IsInGPose);
			}
		}
	}
}
