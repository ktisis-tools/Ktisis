using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.Havok;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Overlay;
using Ktisis.Structs.Input;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Actor.State;

namespace Ktisis.Events {
	public static class EventManager {
		public static bool[] HeldKeys = new bool[255];
		public static bool IsKeyDown(VirtualKey key) => HeldKeys[(int)key] == true;

		public delegate void GPoseChange(ActorGposeState state);
		public static GPoseChange? OnGPoseChange = null;

		public delegate void TransformationMatrixChange(bool state);
		public static TransformationMatrixChange? OnTransformationMatrixChange = null;

		public delegate void GizmoChange(bool isEditing);
		public static GizmoChange? OnGizmoChange = null;

		internal delegate bool KeyPressEventDelegate(QueueItem e);
		internal static KeyPressEventDelegate? OnKeyPressed;

		internal delegate void KeyReleaseEventDelegate(VirtualKey key);
		internal static KeyReleaseEventDelegate? OnKeyReleased;

		internal unsafe delegate void MouseEventDelegate(MouseState* state);
		internal static MouseEventDelegate? OnMouseEvent;

		public static void FireOnGposeChangeEvent(ActorGposeState state) {
			Logger.Debug($"FireOnGposeChangeEvent {state}");
			OnGPoseChange?.Invoke(state);
		}

		public static unsafe void FireOnTransformationMatrixChangeEvent(bool state) {
			if (OnTransformationMatrixChange == null) return;
			var bone = Skeleton.GetSelectedBone();
			var actor = (Actor*)Ktisis.GPoseTarget!.Address;
			hkQsTransformf* boneTransform = bone is null ? &actor->Model->Transform : bone.AccessModelSpace(PropagateOrNot.DontPropagate);
			OnTransformationMatrixChange(state);
		}

		public static void FireOnGizmoChangeEvent(bool isEditing) {
			OnGizmoChange?.Invoke(isEditing);
		}
	}
}
