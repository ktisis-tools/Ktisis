using Dalamud.Game.ClientState.Keys;

using Ktisis.Structs.Input;

namespace Ktisis.Services {
	public static class EventService {
		public delegate void GPoseChange(bool isInGPose);
		public static GPoseChange? OnGPoseChange = null;

		public delegate void TransformationMatrixChange(bool state);
		public static TransformationMatrixChange? OnTransformationMatrixChange = null;

		public delegate void GizmoChange(bool isEditing);
		public static GizmoChange? OnGizmoChange = null;

		internal delegate bool KeyPressEventDelegate(QueueItem e);
		internal static KeyPressEventDelegate? OnKeyPressed;

		internal delegate void KeyReleaseEventDelegate(VirtualKey key);
		internal static KeyReleaseEventDelegate? OnKeyReleased;
	}
}