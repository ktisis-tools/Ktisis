using Dalamud.Game;
using Ktisis.Events;
using System;

namespace Ktisis.Structs.Actor.State {
	[GlobalState]
	public sealed class ActorStateWatcher : IDisposable {

		private static ActorStateWatcher? _instance;

		private ActorGposeState _gposeState = ActorGposeState.OFF;

		public static ActorStateWatcher Instance {
			get {
				if (_instance == null) {
					_instance = new ActorStateWatcher();
				}
				return _instance;
			}
		}

		private ActorStateWatcher() {
			Services.Framework.Update += Monitor;
		}

		public void Dispose() {
			Services.Framework.Update -= Monitor;
			if(Ktisis.IsInGPose)
				EventManager.FireOnGposeChangeEvent(ActorGposeState.OFF);
		}

		[GlobalInit]
		public static void GlobalInit() {
			_ = Instance;
		}

		public void Monitor(Framework framework) {
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
