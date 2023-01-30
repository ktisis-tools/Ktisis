using Dalamud.Game.ClientState.Keys;

using Ktisis.Structs.Input;

namespace Ktisis.Services {
	public static class EventService {
		public delegate void GPoseChange(bool isInGPose);
		public static GPoseChange? OnGPoseChange;

		public delegate void TransformationMatrixChange(bool state);
		public static TransformationMatrixChange? OnTransformationMatrixChange;

		public delegate void GizmoChange(bool isEditing);
		public static GizmoChange? OnGizmoChange;

		internal delegate bool KeyPressEvent(QueueItem e);
		internal static KeyPressEvent? OnKeyPressed;

		internal delegate void KeyReleaseEvent(VirtualKey key);
		internal static KeyReleaseEvent? OnKeyReleased;

		internal delegate void FrameworkUpdate();
		internal static FrameworkUpdate? OnFrameworkUpdate;

		// Init & Dispose

		internal static void Init()
			=> DalamudServices.Framework.Update += InvokeFrameworkUpdate;

		internal static void Dispose()
			=> DalamudServices.Framework.Update -= InvokeFrameworkUpdate;

		// Listen to framework updates

		private static bool _GPoseState = false;

		private static void InvokeFrameworkUpdate(object _) {
			var inGpose = GPoseService.IsInGPose;
			if (inGpose != _GPoseState) {
				_GPoseState = inGpose;
				if (OnGPoseChange != null)
					OnGPoseChange(inGpose);
			}

			if (OnFrameworkUpdate != null)
				OnFrameworkUpdate();
		}
	}
}