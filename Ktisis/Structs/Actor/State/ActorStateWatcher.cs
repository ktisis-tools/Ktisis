using Dalamud.Game;
using Ktisis.Events;
using System;

namespace Ktisis.Structs.Actor.State {
	public static class ActorStateWatcher {

		private static ActorGposeState _gposeState = ActorGposeState.OFF;

		public static void Dispose() {
			Services.Framework.Update -= Monitor;
			if(Ktisis.IsInGPose)
				EventManager.FireOnGposeChangeEvent(ActorGposeState.OFF);
		}

		public static void Init() {
			Services.Framework.Update += Monitor;
		}

		public static void Monitor(Framework framework) {
			if (_gposeState == ActorGposeState.OFF && Ktisis.IsInGPose) {
				_gposeState = ActorGposeState.ON;
				EventManager.FireOnGposeChangeEvent(_gposeState);
			}

			if (_gposeState == ActorGposeState.ON && !Ktisis.IsInGPose) {
				_gposeState = ActorGposeState.OFF;
				EventManager.FireOnGposeChangeEvent(_gposeState);
			}
		}
	}
}
