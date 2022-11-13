using Ktisis.Structs.Actor.State;

namespace Ktisis.Events {
	public static class EventManager {
		public unsafe delegate void GPoseChange(ActorGposeState state);
		public static GPoseChange? OnGPoseChange = null;

		public static void FireOnGposeChangeEvent(ActorGposeState state) {
			OnGPoseChange?.Invoke(state);
		}
	}
}
