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

		internal delegate bool KeyPressEvent(QueueItem e);
		internal static KeyPressEvent? OnKeyPressed;

		internal delegate void KeyReleaseEvent(VirtualKey key);
		internal static KeyReleaseEvent? OnKeyReleased;

		internal delegate void FrameworkUpdate();
		internal static FrameworkUpdate OnFrameworkUpdate = null!;

		internal static void Init()
			=> DalamudServices.Framework.Update += InvokeFrameworkUpdate;

		internal static void Dispose()
			=> DalamudServices.Framework.Update -= InvokeFrameworkUpdate;

		private static void InvokeFrameworkUpdate(object _) {
			OnFrameworkUpdate();
		}
	}
}