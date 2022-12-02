using Dalamud.Game;
using Dalamud.Logging;
using Ktisis.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Ktisis.Structs.Actor.State {
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
				EventManager.FireOnGposeChangeEvent(ActorGposeState.OFF); ;
		}

		public static void Init() {
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
