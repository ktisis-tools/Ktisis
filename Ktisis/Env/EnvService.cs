using Ktisis.Events;
using Ktisis.Interop.Hooks;

namespace Ktisis.Env {
	public static class EnvService {
		public static float? TimeOverride;
		public static uint? SkyOverride;
		
		// Init & Dispose
		
		public static void Init() {
			EventManager.OnGPoseChange += OnGPoseChange;
			EnvHooks.Init();
		}

		public static void Dispose() {
			EventManager.OnGPoseChange -= OnGPoseChange;
			EnvHooks.Dispose();
		}
		
		// Events
		
		private static void OnGPoseChange(bool state) {
			EnvHooks.SetEnabled(state);
			if (!state) {
				TimeOverride = null;
				SkyOverride = null;
			}
		}
	}
}
